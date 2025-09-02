using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static OpenIddict.Server.OpenIddictServerEvents;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Persistence;
using OpenIddict.Abstractions;

namespace Avancira.Infrastructure.Identity;

public static class OpenIddictSetup
{
    public static IServiceCollection AddOpenIddictServer(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOpenIddict()
            .AddCore(options =>
                options.UseEntityFrameworkCore()
                       .UseDbContext<AvanciraDbContext>())
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris(AuthConstants.Endpoints.Authorize)
                       .SetTokenEndpointUris(AuthConstants.Endpoints.Token)
                       .SetRevocationEndpointUris(AuthConstants.Endpoints.Revocation)
                       .SetIssuer(new Uri(configuration["Auth:Issuer"]!)); // e.g. https://api.avancira.com/

                options.AllowAuthorizationCodeFlow()
                       .AllowRefreshTokenFlow()
                       .RequireProofKeyForCodeExchange();

                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.OfflineAccess);

                // Replace with real certs in production
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                options.UseAspNetCore();
                      // .EnableAuthorizationEndpointPassthrough()
                       //.EnableTokenEndpointPassthrough()
                       //.EnableEndSessionEndpointPassthrough();

                // Custom handlers you referenced
                options.AddEventHandler<HandleAuthorizationRequestContext>(builder =>
                    builder.UseScopedHandler<LoginRedirectHandler>().SetOrder(int.MinValue));
                options.AddEventHandler<ProcessSignInContext>(builder =>
                    builder.UseScopedHandler<DeviceInfoClaimsHandler>());
                options.AddEventHandler<ProcessSignInContext>(builder =>
                    builder.UseScopedHandler<SessionIdClaimsHandler>());
                options.AddEventHandler<ApplyTokenResponseContext>(builder =>
                    builder.UseScopedHandler<RefreshTokenCookieHandler>());
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
