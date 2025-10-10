using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        var serverSettings = configuration.GetSection("Auth:OpenIddict").Get<OpenIddictServerSettings>() ?? new();

        services.AddOpenIddict()
            .AddCore(options =>
                options.UseEntityFrameworkCore()
                       .UseDbContext<AvanciraDbContext>())
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris(AuthConstants.Endpoints.Authorize)
                       .SetTokenEndpointUris(AuthConstants.Endpoints.Token)
                       .SetRevocationEndpointUris(AuthConstants.Endpoints.Revocation)
                       .SetIntrospectionEndpointUris("/connect/introspect") // For API resource validation
                       .SetIssuer(new Uri(configuration["Auth:Issuer"]!));

                // Flows
                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange() // PKCE for security
                       .AllowRefreshTokenFlow();

                // Token lifetimes
                options.SetAccessTokenLifetime(serverSettings.AccessTokenLifetime)
                       .SetRefreshTokenLifetime(serverSettings.RefreshTokenLifetime)
                       .SetAuthorizationCodeLifetime(serverSettings.AuthorizationCodeLifetime); // Add code lifetime

                options.UseReferenceRefreshTokens();

                // Scopes
                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.OfflineAccess);

                // Certificates (use real ones in production)
                options.AddDevelopmentEncryptionCertificate()
                      .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableRevocationEndpointPassthrough()
                       .DisableTransportSecurityRequirement(); // Only for development
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
