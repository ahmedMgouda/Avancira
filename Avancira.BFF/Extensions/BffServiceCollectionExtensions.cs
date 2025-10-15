using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Polly;
using Polly.Extensions.Http;
using Yarp.ReverseProxy.Transforms;

namespace Avancira.BFF.Extensions;

public static class BffServiceCollectionExtensions
{
    private const string ApiClientName = "api-client";
    private const string TokenBackchannelClientName = "token-backchannel";

    // ========================================================================
    // 1. AUTHENTICATION (Cookie + OpenID Connect)
    // ========================================================================
    public static IServiceCollection AddBffAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var auth = configuration.GetSection("Auth");
        var cookie = configuration.GetSection("Cookie");

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
        {
            o.Cookie.Name = cookie["Name"] ?? ".Avancira.BFF";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Strict;
            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.SlidingExpiration = true;
            o.ExpireTimeSpan = TimeSpan.FromHours(2);

            // Prevent redirect loops for API calls
            o.Events.OnRedirectToLogin = ctx =>
            {
                if (IsApiRequest(ctx.Request))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            };

            o.Events.OnRedirectToAccessDenied = ctx =>
            {
                if (IsApiRequest(ctx.Request))
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            };
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, o =>
        {
            o.Authority = auth["Authority"] ?? "https://localhost:9100";
            o.ClientId = auth["ClientId"] ?? "bff-client";
            o.ClientSecret = auth["ClientSecret"] ?? "dev-bff-secret";
            o.RequireHttpsMetadata = true; // ✅ enable for production
            o.ResponseType = OpenIdConnectResponseType.Code;
            o.UsePkce = true;

            // Scopes
            o.Scope.Clear();
            o.Scope.Add("openid");
            o.Scope.Add("profile");
            o.Scope.Add("email");
            o.Scope.Add("offline_access");
            o.Scope.Add("api");

            o.SaveTokens = true;
            o.GetClaimsFromUserInfoEndpoint = true;
            o.MapInboundClaims = false;
            o.TokenValidationParameters.NameClaimType = "name";
            o.TokenValidationParameters.RoleClaimType = "role";

            // Log or handle OIDC errors gracefully
            o.Events.OnAuthenticationFailed = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsJsonAsync(new
                {
                    error = "authentication_failed",
                    error_description = ctx.Exception.Message
                });
            };
        });

        return services;
    }

    // ========================================================================
    // 2. TOKEN MANAGEMENT (Duende v4)
    // ========================================================================
    public static IServiceCollection AddBffTokenManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddOpenIdConnectAccessTokenManagement(options =>
        {
            options.RefreshBeforeExpiration = TimeSpan.FromMinutes(5);
        });

        // Token back-channel (used internally by Duende)
        services.AddHttpClient(TokenBackchannelClientName)
            .AddPolicyHandler(GetRetryPolicy());

        // Configure automatic token-injected HttpClient for the API
        var apiBaseAddress = configuration["ReverseProxy:Clusters:api-cluster:Destinations:primary:Address"]
            ?? "https://localhost:9000";

        services.AddUserAccessTokenHttpClient(ApiClientName, configureClient: client =>
        {
            client.BaseAddress = new Uri(apiBaseAddress);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(100);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    // ========================================================================
    // 3. REVERSE PROXY (YARP + Duende token injection)
    // ========================================================================
    public static IServiceCollection AddBffReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms(builderContext =>
            {
                // Automatically attach access token from Duende
                builderContext.AddRequestTransform(async transformContext =>
                {
                    var httpContext = transformContext.HttpContext;
                    if (httpContext.User.Identity?.IsAuthenticated != true)
                        return;

                    var tokenManager = httpContext.RequestServices.GetRequiredService<IUserTokenManager>();
                    var tokenResult = await tokenManager.GetAccessTokenAsync(httpContext.User);

                    if (tokenResult.WasSuccessful(out var token))
                    {
                        transformContext.ProxyRequest.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
                    }
                    else
                    {
                        var logger = httpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Failed to attach access token: {Error}", tokenResult.FailedResult?.Error);
                    }
                });

                // Clean response headers
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

    // ========================================================================
    // 4. AUTHORIZATION
    // ========================================================================
    public static IServiceCollection AddBffAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("authenticated", policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy("api-access", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", "api");
            });

            options.FallbackPolicy = options.GetPolicy("authenticated");
        });

        return services;
    }

    // ========================================================================
    // Helper Utilities
    // ========================================================================
    private static bool IsApiRequest(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api") ||
               request.Path.StartsWithSegments("/bff") ||
               request.ContentType?.Contains("application/json") == true ||
               request.Headers.Accept.Any(h => h?.Contains("application/json") == true);
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
