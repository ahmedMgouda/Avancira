using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Server;
using OpenIddict.Server.Events;
using OpenIddict.Validation.AspNetCore;

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
                       .RequireProofKeyForCodeExchange();

                options.RegisterScopes("api");

                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableRevocationEndpointPassthrough();

                options.AddEventHandler<HandleAuthorizationRequestContext>(builder =>
                    builder.UseScopedHandler<ExternalLoginHandler>());
                options.AddEventHandler<ProcessSignInContext>(builder =>
                    builder.UseScopedHandler<DeviceInfoClaimsHandler>());
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

