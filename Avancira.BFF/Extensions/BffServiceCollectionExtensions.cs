using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
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

            o.Cookie.SameSite = SameSiteMode.None;

            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.SlidingExpiration = true;
            o.ExpireTimeSpan = TimeSpan.FromHours(2);

            // Custom redirect handling for Angular SPA calls
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
            o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            o.Authority = auth["Authority"] ?? "https://localhost:9100";
            o.ClientId = auth["ClientId"] ?? "bff-client";
            o.ClientSecret = auth["ClientSecret"] ?? "dev-bff-secret";
            o.RequireHttpsMetadata = false; // ✅ set true in production

            o.ResponseType = OpenIdConnectResponseType.Code;
            o.UsePkce = true;

            o.CallbackPath = new PathString("/bff/signin-oidc");
            o.SignedOutCallbackPath = new PathString("/bff/signout-callback-oidc");

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

            // Add comprehensive logging for debugging
            o.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProvider = ctx =>
                {
                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("🔵 [OIDC] Redirecting to Identity Provider");
                    logger.LogDebug("🔵 [OIDC] Redirect URI: {RedirectUri}", ctx.ProtocolMessage.RedirectUri);
                    return Task.CompletedTask;
                },

                OnAuthorizationCodeReceived = ctx =>
                {
                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("🟢 [OIDC] Authorization code received from Auth Server");
                    return Task.CompletedTask;
                },

                OnTokenResponseReceived = ctx =>
                {
                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("🟢 [OIDC] Token response received. Expires in: {ExpiresIn}s",
                        ctx.TokenEndpointResponse.ExpiresIn);
                    return Task.CompletedTask;
                },

                OnTokenValidated = ctx =>
                {
                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var sub = ctx.Principal?.FindFirst("sub")?.Value;
                    var name = ctx.Principal?.FindFirst("name")?.Value;
                    logger.LogInformation("🟢 [OIDC] Token validated successfully for user: {Name} (sub: {Sub})",
                        name, sub);
                    return Task.CompletedTask;
                },
                OnTicketReceived = async ctx =>
                {
                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var schemeProvider = ctx.HttpContext.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                    var cookieOptionsMonitor = ctx.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();

                    var cookieScheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();
                    var cookieOptions = cookieOptionsMonitor.Get(cookieScheme?.Name ?? CookieAuthenticationDefaults.AuthenticationScheme);

                    logger.LogInformation("🟢 [OIDC] Authentication ticket received");
                    logger.LogInformation("🍪 [OIDC] Cookie settings: Name={Name}, SameSite={SameSite}, Secure={Secure}, HttpOnly={HttpOnly}",
                        cookieOptions.Cookie.Name,
                        cookieOptions.Cookie.SameSite,
                        cookieOptions.Cookie.SecurePolicy,
                        cookieOptions.Cookie.HttpOnly);

                    logger.LogInformation("🔄 [OIDC] Redirecting user to: {ReturnUri}", ctx.ReturnUri);
                },

                OnRemoteFailure = ctx =>
                {
                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ctx.Failure, "🔴 [OIDC] Remote authentication failure");

                    ctx.HandleResponse();
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    ctx.Response.ContentType = "application/json";
                    return ctx.Response.WriteAsJsonAsync(new
                    {
                        error = "authentication_failed",
                        error_description = ctx.Failure?.Message ?? "Remote authentication failed"
                    });
                },

                OnAuthenticationFailed = ctx =>
                {
                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ctx.Exception, "🔴 [OIDC] Authentication failed");

                    ctx.HandleResponse();
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    ctx.Response.ContentType = "application/json";
                    return ctx.Response.WriteAsJsonAsync(new
                    {
                        error = "authentication_failed",
                        error_description = ctx.Exception.Message
                    });
                }
            };
        });

        return services;
    }

    // ========================================================================
    // 2. TOKEN MANAGEMENT (Duende)
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

        // Token back-channel (Duende internal)
        services.AddHttpClient(TokenBackchannelClientName)
            .AddPolicyHandler(GetRetryPolicy());

        // HttpClient used by YARP or app services with user token injection
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
                // Inject user access token for proxied API requests
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

                // ✅ Clean up unnecessary response headers
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
        services.AddAuthorization();
        return services;
    }

    // ========================================================================
    // Helper Utilities
    // ========================================================================
    private static bool IsApiRequest(HttpRequest request)
    {
        var path = request.Path;

        // Treat all /bff/... as SPA/AJAX calls
        // but allow redirects for login/logout flows
        if (path.StartsWithSegments("/bff", StringComparison.OrdinalIgnoreCase))
        {
            if (path.StartsWithSegments("/bff/auth/login", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWithSegments("/bff/auth/logout", StringComparison.OrdinalIgnoreCase))
            {
                return false; // Allow browser redirects
            }

            return true; // All other /bff/* return 401/403 for SPA
        }

        // Non-/bff requests can redirect normally
        return false;
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
