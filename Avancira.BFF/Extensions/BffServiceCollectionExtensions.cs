using Avancira.BFF.Services;
using IdentityModel.AspNetCore.AccessTokenManagement;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace Avancira.BFF.Extensions;

public static class BffServiceCollectionExtensions
{
    public static IServiceCollection AddBffAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authConfig = configuration.GetSection("Auth");
        var cookieConfig = configuration.GetSection("Cookie");

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.Cookie.Name = cookieConfig["Name"] ?? ".Avancira.BFF";
            options.Cookie.SameSite = Enum.TryParse(cookieConfig["SameSite"], out SameSiteMode sameSite)
                ? sameSite
                : SameSiteMode.Strict;
            options.Cookie.SecurePolicy = Enum.TryParse(cookieConfig["SecurePolicy"], out CookieSecurePolicy securePolicy)
                ? securePolicy
                : CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true;

            if (TimeSpan.TryParse(cookieConfig["ExpireTimeSpan"], out var lifetime))
            {
                options.ExpireTimeSpan = lifetime;
            }

            options.SlidingExpiration = !bool.TryParse(cookieConfig["SlidingExpiration"], out var sliding) || sliding;

            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.TryAdd("X-Unauthenticated", "true");
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        })
        .AddOpenIdConnect(options =>
        {
            options.Authority = authConfig["Authority"];
            options.ClientId = authConfig["ClientId"];
            options.ClientSecret = authConfig["ClientSecret"];
            options.RequireHttpsMetadata = !bool.TryParse(authConfig["RequireHttpsMetadata"], out var requireHttps) || requireHttps;

            options.ResponseType = OpenIdConnectResponseType.Code;
            options.ResponseMode = OpenIdConnectResponseMode.Query;
            options.UsePkce = true;

            options.Scope.Clear();
            var scopes = authConfig.GetSection("Scopes").Get<string[]>() ?? new[] { "openid", "profile", "email", "offline_access" };
            foreach (var scope in scopes)
            {
                options.Scope.Add(scope);
            }

            options.SaveTokens = !bool.TryParse(authConfig["SaveTokens"], out var saveTokens) || saveTokens;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.MapInboundClaims = false;
            options.TokenValidationParameters.NameClaimType = "name";
            options.TokenValidationParameters.RoleClaimType = "role";

            options.CallbackPath = "/signin-oidc";
            options.SignedOutCallbackPath = "/signout-callback-oidc";

            options.Events = new OpenIdConnectEvents
            {
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var userId = context.Principal?.FindFirst("sub")?.Value;
                    logger.LogInformation("User {UserId} authenticated via OIDC", userId);
                    return Task.CompletedTask;
                },
                OnRemoteFailure = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(context.Failure, "OIDC authentication failed: {Message}", context.Failure?.Message);
                    context.Response.Redirect("/");
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    public static IServiceCollection AddBffTokenManagement(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddHttpClient();
        services.AddSingleton<ITokenManagementService, TokenManagementService>();

        var tokenConfig = configuration.GetSection("TokenManagement");
        var refreshBefore = TimeSpan.TryParse(tokenConfig["RefreshBeforeExpiration"], out var parsed)
            ? parsed
            : TimeSpan.FromMinutes(5);
        var enableAutomaticRefresh = !bool.TryParse(tokenConfig["EnableAutomaticRefresh"], out var automatic) || automatic;

        services.AddAccessTokenManagement(options =>
        {
            options.User.RefreshBeforeExpiration = refreshBefore;
            options.User.AutoRefresh = enableAutomaticRefresh;
        })
        .ConfigureBackchannelHttpClient()
        .AddPolicyHandler(GetRetryPolicy());

        services.AddOptions<AccessTokenManagementOptions>()
            .Configure(options =>
            {
                options.User.RefreshBeforeExpiration = refreshBefore;
                options.User.AutoRefresh = enableAutomaticRefresh;
            });

        services.AddSession(options =>
        {
            options.Cookie.Name = ".Avancira.BFF.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.IdleTimeout = TimeSpan.FromHours(8);
        });

        return services;
    }

    public static IServiceCollection AddBffCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration["Cors:AllowedOrigins"]?
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? new[] { "http://localhost:4200" };

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("X-Token-Expired", "X-Unauthenticated");
            });
        });

        return services;
    }

    public static IServiceCollection AddBffReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms(builderContext =>
            {
                builderContext.AddRequestTransform(async transformContext =>
                {
                    if (transformContext.HttpContext.User.Identity?.IsAuthenticated != true)
                    {
                        return;
                    }

                    var tokenService = transformContext.HttpContext.RequestServices.GetRequiredService<IUserAccessTokenManagementService>();
                    var logger = transformContext.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                    try
                    {
                        var tokenResult = await tokenService.GetUserAccessTokenAsync(transformContext.HttpContext.User);
                        if (!string.IsNullOrEmpty(tokenResult.AccessToken))
                        {
                            transformContext.ProxyRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);
                        }
                        else if (!string.IsNullOrEmpty(tokenResult.Error))
                        {
                            logger.LogWarning("Failed to acquire access token: {Error}", tokenResult.Error);
                            transformContext.HttpContext.Response.Headers.TryAdd("X-Token-Expired", "true");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error acquiring access token for proxy request");
                    }
                });

                builderContext.AddResponseTransform(transformContext =>
                {
                    transformContext.ProxyResponse?.Headers.Remove("Set-Cookie");
                    return ValueTask.CompletedTask;
                });
            });

        return services;
    }

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
        });

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() => HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(3)
        });
}
