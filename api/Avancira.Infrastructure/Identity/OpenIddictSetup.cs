using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Avancira.Infrastructure.Identity;

public static class OpenIddictSetup
{
    public static IServiceCollection AddOpenIddictServer(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOpenIddict()
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetRevocationEndpointUris("/connect/revocation")
                       .SetIssuer(new Uri(configuration["Auth:Issuer"]!));

                options.AllowRefreshTokenFlow()
                       .AllowAuthorizationCodeFlow()
                       .AllowPasswordFlow()
                       .RequireProofKeyForCodeExchange();

                options.RegisterScopes("api");

                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough();

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

        services.AddOpenIddictServer(configuration);

        return services;
    }
}

