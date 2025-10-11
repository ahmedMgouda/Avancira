using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Persistence;
using OpenIddict.Abstractions;

namespace Avancira.Infrastructure.Identity;

public static class OpenIddictSetup
{
    /// <summary>
    /// CHANGE 1: Configures OpenIddict SERVER for the Auth MVC project
    /// This issues tokens - only used by the Auth server
    /// </summary>
    public static IServiceCollection AddOpenIddictServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var serverSettings = configuration.GetSection("Auth:OpenIddict")
            .Get<OpenIddictServerSettings>() ?? new();

        var issuer = configuration["Auth:Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException(
                "Auth:Issuer configuration is required for OpenIddict. " +
                "This should be the public URL of your Auth server.");
        }

        services.AddOpenIddict()
            // ===== Core Configuration (Database) =====
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<AvanciraDbContext>();
            })

            // ===== Server Configuration (Token Issuing) =====
            .AddServer(options =>
            {
                // CHANGE 2: Configure all required endpoints
                options.SetAuthorizationEndpointUris(AuthConstants.Endpoints.Authorize)
                       .SetTokenEndpointUris(AuthConstants.Endpoints.Token)
                       .SetRevocationEndpointUris(AuthConstants.Endpoints.Revocation)
                       .SetUserInfoEndpointUris("/connect/userinfo")
                       .SetIntrospectionEndpointUris("/connect/introspect");

                // CHANGE 3: Set issuer (must match what clients expect)
                options.SetIssuer(new Uri(issuer));

                // CHANGE 4: Enable authorization code flow with PKCE
                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange(); // PKCE for security

                // CHANGE 5: Enable refresh token flow
                options.AllowRefreshTokenFlow();

                // CHANGE 6: Configure token lifetimes
                options.SetAccessTokenLifetime(serverSettings.AccessTokenLifetime)
                       .SetRefreshTokenLifetime(serverSettings.RefreshTokenLifetime)
                       .SetAuthorizationCodeLifetime(serverSettings.AuthorizationCodeLifetime);

                // CHANGE 7: Use reference tokens for refresh tokens (more secure)
                // Reference tokens can be revoked, whereas JWT refresh tokens cannot
                options.UseReferenceRefreshTokens();

                // CHANGE 8: Register scopes that clients can request
                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.OfflineAccess);

                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                // CHANGE 11: ASP.NET Core integration
                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough();
            })

            // ===== CHANGE 14: Add validation for local token validation =====
            // This allows the Auth server to validate its own tokens if needed
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }

    /// <summary>
    /// CHANGE 15: Main entry point for configuring OpenIddict in Auth project
    /// Call this from your Auth project's Program.cs/Startup.cs
    /// </summary>
    public static IServiceCollection AddInfrastructureIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure OpenIddict server for token issuing
        services.AddOpenIddictServer(configuration);

        return services;
    }
}