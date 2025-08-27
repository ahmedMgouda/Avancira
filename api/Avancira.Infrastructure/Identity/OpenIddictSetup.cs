using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using System.Linq;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Avancira.Infrastructure.Identity;

public static class OpenIddictSetup
{
    public static IServiceCollection AddOpenIddictServer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenIddict()
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetRevocationEndpointUris("/connect/revocation")
                       .SetIssuer(new Uri("https://localhost:9000/"));

                options.AllowRefreshTokenFlow()
                       .AllowAuthorizationCodeFlow()
                       .AllowPasswordFlow()
                       .RequireProofKeyForCodeExchange();

                options.RegisterScopes("api", OpenIddictConstants.Scopes.OfflineAccess);

                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough();

                options.AddEventHandler<ValidateAuthorizationRequestContext>(builder =>
                    builder.UseInlineHandler(context =>
                    {
                        context.Scopes.UnionWith(context.Request.GetScopes()
                            .Where(scope => scope is "api" or OpenIddictConstants.Scopes.OfflineAccess));
                        return default;
                    }));
                options.AddEventHandler<ValidateTokenRequestContext>(builder =>
                    builder.UseInlineHandler(context =>
                    {
                        context.Scopes.UnionWith(context.Request.GetScopes()
                            .Where(scope => scope is "api" or OpenIddictConstants.Scopes.OfflineAccess));
                        return default;
                    }));
                options.AddEventHandler<HandleAuthorizationRequestContext>(builder =>
                    builder.UseScopedHandler<ExternalLoginHandler>());
                options.AddEventHandler<ProcessSignInContext>(builder =>
                    builder.UseScopedHandler<DeviceInfoClaimsHandler>());
                options.AddEventHandler<HandleTokenRequestContext>(builder =>
                    builder.UseScopedHandler<PasswordGrantHandler>());
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }

    public static IServiceCollection AddInfrastructureIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOpenIddictServer();

        return services;
    }
}

