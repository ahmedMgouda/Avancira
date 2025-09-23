using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static OpenIddict.Server.OpenIddictServerEvents;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Persistence;
using OpenIddict.Abstractions;
using Avancira.Infrastructure.Identity.Handlers;

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
                // Default endpoints - OpenIddict handles the protocol implementation
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
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(2))  // Reasonable for production
                       .SetRefreshTokenLifetime(TimeSpan.FromMinutes(3))
                       .SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(5)); // Add code lifetime


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


                // Use default endpoints (no passthrough needed)
                options.UseAspNetCore()
                       .DisableTransportSecurityRequirement(); // Only for development

                // Your custom event handlers - these work with default endpoints
                options.AddEventHandler<HandleAuthorizationRequestContext>(builder =>
                    builder.UseScopedHandler<LoginRedirectHandler>()
                           .SetOrder(int.MinValue));

                options.AddEventHandler<ProcessSignInContext>(builder =>
                    builder.UseScopedHandler<SessionIdClaimsHandler>());

                options.AddEventHandler<ApplyTokenResponseContext>(builder =>
                    builder.UseScopedHandler<RefreshTokenCookieHandler>());

                options.AddEventHandler<ApplyTokenResponseContext>(builder =>
                    builder.UseScopedHandler<IssueSessionHandler>());

                options.AddEventHandler<ProcessSignInContext>(builder =>
                 builder.UseScopedHandler<PerDeviceAuthorizationHandler>());

                // Uncomment if you need to handle refresh tokens from cookies
                // options.AddEventHandler<ProcessAuthenticationContext>(builder =>
                //     builder.UseScopedHandler<RefreshTokenFromCookieHandler>());
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

        // Register your custom handlers
        services.AddScoped<LoginRedirectHandler>();
        services.AddScoped<SessionIdClaimsHandler>();
        services.AddScoped<RefreshTokenCookieHandler>();
        services.AddScoped<IssueSessionHandler>();
        services.AddScoped<PerDeviceAuthorizationHandler>();
        //services.AddScoped<SessionActivityHandler>();
        // services.AddScoped<RefreshTokenFromCookieHandler>();

        // Add session management
        //services.AddSessionManagement(configuration);

        return services;
    }
}