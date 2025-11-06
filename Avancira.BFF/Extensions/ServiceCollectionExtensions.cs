namespace Avancira.BFF.Extensions;

using Avancira.BFF.Configuration;
using Avancira.BFF.Services;
using Avancira.Infrastructure.Health;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all BFF services
    /// </summary>
    public static IServiceCollection AddBffServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Bind and register settings
        var settings = configuration.Get<BffSettings>() ?? new BffSettings();
        settings.RedisConnection = configuration.GetConnectionString("Redis");
        settings.ApiBaseUrl = configuration["ReverseProxy:Clusters:api-cluster:Destinations:primary:Address"]
            ?? settings.ApiBaseUrl;

        services.AddSingleton(settings);

        // Core services
        services.AddHttpContextAccessor();

        // Feature services
        services.AddDistributedCacheService(settings);
        services.AddCorsService(settings);
        services.AddAuthenticationService(settings);
        services.AddTokenManagementService(settings);
        services.AddReverseProxyService(configuration, settings);
        services.AddAuthorizationService();

        // MVC & API
        services.AddControllers();

        var healthChecksBuilder = services.AddAvanciraHealthChecks(configuration, options =>
        {
            options.CheckDatabase = false;
        });

        ConfigureDownstreamHealthChecks(services, configuration, settings, healthChecksBuilder);

        // Swagger (development only)
        if (environment.IsDevelopment())
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new()
                {
                    Title = "Avancira BFF API",
                    Version = "v1",
                    Description = "Backend-for-Frontend with minimal cookie (sub + sid only)"
                });
            });
        }

        return services;
    }

    private static void ConfigureDownstreamHealthChecks(
        IServiceCollection services,
        IConfiguration configuration,
        BffSettings settings,
        IHealthChecksBuilder healthChecksBuilder)
    {
        var downstreamSection = configuration.GetSection("HealthCheck:Downstream");
        if (!downstreamSection.Exists())
        {
            return;
        }

        var endpoints = new List<(DownstreamEndpoint Endpoint, HealthStatus FailureStatus)>();

        foreach (var child in downstreamSection.GetChildren())
        {
            var enabled = child.GetValue<bool?>("Enabled");
            if (enabled.HasValue && !enabled.Value)
            {
                continue;
            }

            var name = child.Key.ToLowerInvariant();
            var url = child.GetValue<string>("Url") ?? ResolveDefaultUrl(name, settings);
            var healthPath = child.GetValue<string>("HealthPath") ?? "health/live";

            var endpoint = DownstreamEndpoint.Create(name, url, healthPath);

            var timeoutSeconds = child.GetValue<int?>("Timeout");
            if (timeoutSeconds.HasValue && timeoutSeconds.Value > 0)
            {
                endpoint.Timeout = TimeSpan.FromSeconds(timeoutSeconds.Value);
            }

            endpoints.Add((endpoint, ParseHealthStatus(child.GetValue<string>("FailureStatus"))));
        }

        if (endpoints.Count == 0)
        {
            return;
        }

        var httpClientTimeout = endpoints
            .Select(e => e.Endpoint.Timeout)
            .Where(t => t.HasValue && t.Value > TimeSpan.Zero)
            .Select(t => t!.Value)
            .DefaultIfEmpty(TimeSpan.FromSeconds(5))
            .Max();

        services.AddHttpClient("downstream-health-check", client =>
        {
            client.Timeout = httpClientTimeout;
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
            };
            client.DefaultRequestHeaders.Add("User-Agent", "Avancira-BFF-HealthCheck/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

        services.AddSingleton(new DownstreamHealthCheckOptions
        {
            HttpClientName = "downstream-health-check",
            Endpoints = endpoints.Select(e => e.Endpoint).ToList()
        });

        var aggregatedFailureStatus = endpoints.Any(e => e.FailureStatus == HealthStatus.Unhealthy)
            ? HealthStatus.Unhealthy
            : endpoints.Any(e => e.FailureStatus == HealthStatus.Degraded)
                ? HealthStatus.Degraded
                : HealthStatus.Unhealthy;

        healthChecksBuilder.AddCheck<DownstreamHealthCheck>(
            "downstream_services",
            failureStatus: aggregatedFailureStatus,
            tags: new[] { "ready", "downstream" });
    }

    private static string? ResolveDefaultUrl(string name, BffSettings settings)
        => name switch
        {
            "api" => settings.ApiBaseUrl,
            "auth" => settings.Auth.Authority,
            _ => null
        };

    private static HealthStatus ParseHealthStatus(string? value)
        => Enum.TryParse<HealthStatus>(value, ignoreCase: true, out var parsed)
            ? parsed
            : HealthStatus.Unhealthy;

    /// <summary>
    /// Registers distributed cache (Redis or Memory)
    /// </summary>
    private static IServiceCollection AddDistributedCacheService(
        this IServiceCollection services,
        BffSettings settings)
    {
        if (settings.HasRedis)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = settings.RedisConnection;
                options.InstanceName = "BFF_";
            });
            Console.WriteLine("Redis cache configured");
        }
        else
        {
            services.AddDistributedMemoryCache();
            Console.WriteLine("In-memory cache (dev only)");
        }

        return services;
    }

    /// <summary>
    /// Registers CORS policy
    /// </summary>
    private static IServiceCollection AddCorsService(
        this IServiceCollection services,
        BffSettings settings)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(settings.Cors.AllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetIsOriginAllowedToAllowWildcardSubdomains();
            });
        });

        return services;
    }

    /// <summary>
    /// Registers authentication (Cookie + OpenIdConnect)
    /// STRATEGY: Minimal cookie with only sub + sid claims
    /// </summary>
    private static IServiceCollection AddAuthenticationService(
        this IServiceCollection services,
        BffSettings settings)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name = settings.Cookie.Name;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.MaxAge = TimeSpan.FromHours(settings.Cookie.ExpirationHours);

            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(settings.Cookie.ExpirationHours);

            // Event handlers from separate service
            options.Events = CookieEventHandlers.CreateEvents(settings);
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            options.Authority = settings.Auth.Authority;
            options.ClientId = settings.Auth.ClientId;
            options.ClientSecret = settings.Auth.ClientSecret;
            options.RequireHttpsMetadata = settings.Auth.RequireHttpsMetadata;

            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;

            // Scopes to request
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("api");
            options.Scope.Add("offline_access");

            // Callback paths 
            options.CallbackPath = "/bff/signin-oidc";
            options.SignedOutCallbackPath = "/bff/signout-callback-oidc";
            options.SignedOutRedirectUri = "https://localhost:4200/";

            // CRITICAL: Tokens stored server-side by Duende
            options.SaveTokens = true;

            // Don't fetch from UserInfo endpoint (data is in access token)
            options.GetClaimsFromUserInfoEndpoint = false;
            options.MapInboundClaims = false;
            options.DisableTelemetry = true;

            options.TokenValidationParameters.NameClaimType = "name";
            options.TokenValidationParameters.RoleClaimType = "role";
            options.TokenValidationParameters.ValidateAudience = true;
            options.TokenValidationParameters.ValidateIssuer = true;

            options.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProviderForSignOut = context =>
                {
                    var idToken = context.Properties.GetTokenValue("id_token");
                    if (!string.IsNullOrEmpty(idToken))
                    {
                        context.ProtocolMessage.IdTokenHint = idToken;
                    }
                    return Task.CompletedTask;
                }
            };
            // Event handlers from separate service
            options.Events = OidcEventHandlers.CreateEvents();
        });

        return services;
    }

    /// <summary>
    /// Registers Duende token management
    /// STRATEGY: Server-side token storage (Redis/Memory)
    /// </summary>
    private static IServiceCollection AddTokenManagementService(
        this IServiceCollection services,
        BffSettings settings)
    {
        services.AddOpenIdConnectAccessTokenManagement(options =>
        {
            // Refresh tokens 5 minutes before expiry
            options.RefreshBeforeExpiration = TimeSpan.FromMinutes(5);
        });

        // HTTP client for token backchannel communication
        services.AddHttpClient("token-backchannel")
            .AddPolicyHandler(GetRetryPolicy());

        // HTTP client for API calls (auto token injection)
        services.AddUserAccessTokenHttpClient("api-client", null, client =>
        {
            client.BaseAddress = new Uri(settings.ApiBaseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "Avancira-BFF/1.0");
            client.Timeout = TimeSpan.FromSeconds(100);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Auth server client
        services.AddUserAccessTokenHttpClient("auth-client", null, client =>
        {
            client.BaseAddress = new Uri(settings.Auth.Authority);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "Avancira-BFF/1.0");
            client.Timeout = TimeSpan.FromSeconds(100);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddScoped<ApiClient>();

        return services;
    }

    /// <summary>
    /// Registers YARP reverse proxy
    /// STRATEGY: Automatic token injection for authenticated requests
    /// </summary>
    private static IServiceCollection AddReverseProxyService(
        this IServiceCollection services,
        IConfiguration configuration,
        BffSettings settings)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms(builderContext =>
            {
                // Inject access token
                builderContext.AddRequestTransform(ProxyTransformService.AttachAccessToken);

                // Clean security headers
                builderContext.AddResponseTransform(ProxyTransformService.CleanSecurityHeaders);
            });

        return services;
    }

    /// <summary>
    /// Registers authorization policies
    /// </summary>
    private static IServiceCollection AddAuthorizationService(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Authenticated", policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }

    // ═══════════════════════════════════════════════════════════════════
    // Resilience Policies
    // ═══════════════════════════════════════════════════════════════════

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s");
                });

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
}
