using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Avancira.Infrastructure.Persistence;
using OpenIddict.Abstractions;

namespace Avancira.Auth.OpenIddict;

/// <summary>
/// FIXED: Proper OpenIddict server configuration with all required scopes
/// This configures the Auth server to issue tokens with correct scopes and claims
/// </summary>
public static class OpenIddictSetup
{
    /// <summary>
    /// Configures OpenIddict SERVER for the Auth MVC project
    /// This server ISSUES tokens - only used by the Auth server
    /// 
    /// Do NOT use this in the BFF project
    /// BFF uses OpenIddict CLIENT to validate tokens
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
                "Example: 'https://localhost:5005' (local dev) or 'https://yourdomain.com/auth' (production)");
        }

        services.AddOpenIddict()
            // ===== CORE: Database Configuration =====
            // OpenIddict stores clients, tokens, scopes in database
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<AvanciraDbContext>();
            })

            // ===== SERVER: Token Issuance Configuration =====
            .AddServer(options =>
            {
                // Configure endpoints
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetRevocationEndpointUris("/connect/revoke")
                       .SetIntrospectionEndpointUris("/connect/introspect")
                       .SetEndSessionEndpointUris("/connect/logout")
                       .SetUserInfoEndpointUris("/connect/userinfo");


                // CRITICAL: Set issuer (must match what clients expect)
                options.SetIssuer(new Uri(issuer));

                // ===== FLOW CONFIGURATION =====
                // Authorization Code Flow: Browser → Auth Server → Token
                // This is the recommended flow for SPAs and native apps
                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange() // PKCE: Prevents code interception
                       .AllowRefreshTokenFlow()
                       .AllowClientCredentialsFlow();

                // ===== TOKEN LIFETIME CONFIGURATION =====
                // Access Token: Short-lived (15 minutes)
                // Refresh Token: Long-lived (14 days)
                // Authorization Code: Very short (10 minutes)
                options.SetAccessTokenLifetime(serverSettings.AccessTokenLifetime)
                       .SetRefreshTokenLifetime(serverSettings.RefreshTokenLifetime)
                       .SetAuthorizationCodeLifetime(serverSettings.AuthorizationCodeLifetime);

                // CRITICAL SECURITY: Use reference refresh tokens
                // Reference tokens can be revoked (unlike JWT tokens)
                // When user logs out, we revoke the reference
                options.UseReferenceRefreshTokens();
                options.UseReferenceAccessTokens();

                // ===== SCOPE CONFIGURATION =====
                // FIXED: Added "api" scope for API access
                // Clients must request one of these scopes
                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,      // Get user ID
                    OpenIddictConstants.Scopes.Profile,     // Get user profile (name, etc)
                    OpenIddictConstants.Scopes.Email,       // Get email address
                    OpenIddictConstants.Scopes.OfflineAccess, // Get refresh token
                    "api");                                 // FIXED: Access to backend API

                // ===== CRYPTOGRAPHY =====
                // For development: Generate temporary certificates
                // For production: Use real certificates from key vault
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                // ===== ASP.NET CORE INTEGRATION =====
                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()  // Allows custom logic
                       .EnableTokenEndpointPassthrough()          // Allows custom logic
                       .EnableEndSessionEndpointPassthrough();     // Allows custom logic
            })

            // ===== VALIDATION: Token Validation Configuration =====
            // Allows this Auth server to validate its own tokens if needed
            .AddValidation(options =>
            {
                options.UseLocalServer();  // Use local configuration
                options.UseAspNetCore();   // ASP.NET Core integration
            });


        return services;
    }

    /// <summary>
    /// Main entry point for configuring OpenIddict in Auth project
    /// Call this from your Auth project's Program.cs
    /// 
    /// Example:
    /// builder.Services.AddInfrastructureIdentity(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddInfrastructureIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure OpenIddict server for token issuance
        services.AddOpenIddictServer(configuration);

        return services;
    }
}
