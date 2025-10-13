using System.Security.Claims;
using Avancira.BFF.Services;
using Avancira.Infrastructure.Auth;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Polly;
using Polly.Extensions.Http;
using Yarp.ReverseProxy.Transforms;

namespace Avancira.BFF.Extensions;

/// <summary>
/// Service collection extensions for BFF configuration.
/// Handles authentication, token management, CORS, reverse proxy, and authorization.
/// </summary>
public static class BffServiceCollectionExtensions
{
    // =====================================================================
    // 1. AUTHENTICATION CONFIGURATION
    // =====================================================================
    public static IServiceCollection AddBffAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authConfig = configuration.GetSection("Auth");
        var cookieConfig = configuration.GetSection("Cookie");

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        // ===== COOKIE AUTHENTICATION =====
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name = cookieConfig["Name"] ?? ".Avancira.BFF";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = Enum.TryParse(cookieConfig["SameSite"], out SameSiteMode sameSite)
                ? sameSite
                : SameSiteMode.Strict;
            options.Cookie.SecurePolicy = Enum.TryParse(cookieConfig["SecurePolicy"], out CookieSecurePolicy securePolicy)
                ? securePolicy
                : CookieSecurePolicy.Always;

            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.TryParse(cookieConfig["ExpireTimeSpan"], out var lifetime)
                ? lifetime
                : TimeSpan.FromHours(2);

            // Prevent redirects for API calls
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/bff") ||
                    context.Request.ContentType?.Contains("application/json") == true)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.Headers["X-Unauthenticated"] = "true";
                    return Task.CompletedTask;
                }

                // Default redirect for normal browser requests
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/bff") ||
                    context.Request.ContentType?.Contains("application/json") == true)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        })
        // ===== OPENID CONNECT AUTHENTICATION =====
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = authConfig["Authority"];
            options.ClientId = authConfig["ClientId"];
            options.ClientSecret = authConfig["ClientSecret"];
            options.RequireHttpsMetadata = !bool.TryParse(authConfig["RequireHttpsMetadata"], out var requireHttps) || requireHttps;

            options.ResponseType = OpenIdConnectResponseType.Code;
            options.ResponseMode = OpenIdConnectResponseMode.Query;
            options.UsePkce = true;

            // Configure scopes
            options.Scope.Clear();
            var scopes = authConfig.GetSection("Scopes").Get<string[]>() ?? new[] { "openid", "profile", "email", "offline_access" };
            foreach (var scope in scopes)
                options.Scope.Add(scope);

            // Save tokens so Duende can manage them automatically
            options.SaveTokens = !bool.TryParse(authConfig["SaveTokens"], out var saveTokens) || saveTokens;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.MapInboundClaims = false;
            options.TokenValidationParameters.NameClaimType = "name";
            options.TokenValidationParameters.RoleClaimType = "role";

            options.CallbackPath = "/signin-oidc";
            options.SignedOutCallbackPath = "/signout-callback-oidc";

            // Log events for better diagnostics
            options.Events = new OpenIdConnectEvents
            {
                OnAuthorizationCodeReceived = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("OIDC authorization code received from {Authority}", context.Options.Authority);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var userId = context.Principal?.FindFirst("sub")?.Value ?? "unknown";
                    logger.LogInformation("✅ User {UserId} authenticated via OIDC", userId);
                    return Task.CompletedTask;
                },
                OnRemoteFailure = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(context.Failure, "❌ OIDC authentication failed: {Message}", context.Failure?.Message);
                    context.Response.Redirect("/");
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    // =====================================================================
    // 2. TOKEN MANAGEMENT CONFIGURATION (DUENDE)
    // =====================================================================
    public static IServiceCollection AddBffTokenManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddHttpClient();

        services.AddSingleton<ITokenManagementService, TokenManagementService>();
        services.AddScoped<ISessionManagementService, SessionManagementService>();

        // Correct Duende method
        services.AddOpenIdConnectAccessTokenManagement();

        // Optional resilient backchannel (used for token refresh & revocation)
        services.AddHttpClient("token-backchannel")
            .AddPolicyHandler(GetRetryPolicy());

        // Optional automatic HttpClient with access token injection
        services.AddUserAccessTokenHttpClient("api-client",
        configureClient: client =>
        {
            client.BaseAddress = new Uri(configuration["ReverseProxy:Clusters:api-cluster:Destinations:primary:Address"]
                ?? "https://localhost:5001");
        });

        // Session setup for state/token cache
        var tokenConfig = configuration.GetSection("TokenManagement");
        var minutes = double.TryParse(tokenConfig["SessionIdleTimeoutMinutes"], out var m) ? m : 120;

        services.AddSession(options =>
        {
            options.Cookie.Name = ".Avancira.BFF.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.IdleTimeout = TimeSpan.FromMinutes(minutes);
        });

        return services;
    }

    // =====================================================================
    // 3. CORS CONFIGURATION
    // =====================================================================
    public static IServiceCollection AddBffCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsConfig = configuration.GetSection("Cors");
        var allowedOrigins = corsConfig["AllowedOrigins"]?
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .ToArray();

        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                allowedOrigins = new[] { "http://localhost:4200", "https://localhost:4200" };
            else
                throw new InvalidOperationException("CORS AllowedOrigins must be configured in production.");
        }

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowCredentials()
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .WithExposedHeaders("X-Token-Expired", "X-Unauthenticated", "X-Token-Refresh-Required");
            });
        });

        return services;
    }

    // =====================================================================
    // 4. REVERSE PROXY CONFIGURATION (YARP)
    // =====================================================================
    public static IServiceCollection AddBffReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms(builderContext =>
            {
                // ===== REQUEST TRANSFORM =====
                builderContext.AddRequestTransform(async transformContext =>
                {
                    var httpContext = transformContext.HttpContext;

                    if (httpContext.User.Identity?.IsAuthenticated != true)
                        return;

                    var userId = httpContext.User.FindFirstValue("sub");
                    var sessionIdClaim = httpContext.User.FindFirstValue(AuthConstants.Claims.SessionId);

                    if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(sessionIdClaim, out var sessionId))
                        return;

                    var tokenService = httpContext.RequestServices.GetRequiredService<ITokenManagementService>();
                    var logger = httpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                    try
                    {
                        var tokenResult = await tokenService.GetAccessTokenAsync(userId, sessionId, httpContext.RequestAborted);
                        if (tokenResult.Success && !string.IsNullOrEmpty(tokenResult.Token))
                        {
                            transformContext.ProxyRequest.Headers.Authorization =
                                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.Token);

                            if (tokenResult.NeedsRefresh)
                                httpContext.Response.Headers["X-Token-Refresh-Required"] = "true";
                        }
                        else
                        {
                            logger.LogWarning("⚠️ No valid access token for user {UserId}: {Error}", userId, tokenResult.Error);
                            httpContext.Response.Headers["X-Token-Expired"] = "true";
                            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error acquiring access token for proxy request");
                        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        httpContext.Response.Headers["X-Token-Expired"] = "true";
                        return;
                    }
                });

                // ===== RESPONSE TRANSFORM =====
                builderContext.AddResponseTransform(transformContext =>
                {
                    transformContext.ProxyResponse?.Headers.Remove("Set-Cookie");
                    transformContext.ProxyResponse?.Headers.Remove("Server");
                    transformContext.ProxyResponse?.Headers.Remove("X-Powered-By");
                    return ValueTask.CompletedTask;
                });
            });

        return services;
    }

    // =====================================================================
    // 5. AUTHORIZATION CONFIGURATION
    // =====================================================================
    public static IServiceCollection AddBffAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());
            options.AddPolicy("admin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin");
            });
            options.AddPolicy("device-owner", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new DeviceOwnerRequirement());
            });
        });

        services.AddSingleton<IAuthorizationHandler, DeviceOwnerHandler>();

        return services;
    }

    // =====================================================================
    // 6. HTTP RETRY POLICY
    // =====================================================================
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<HttpRequestException>(ex => ex.InnerException is TimeoutException)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, retryCount, context) => { });
    }
}
