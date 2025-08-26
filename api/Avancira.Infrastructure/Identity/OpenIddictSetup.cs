using Microsoft.AspNetCore.Authentication;
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
                       .SetTokenEndpointUris("/connect/token");

                options.AllowPasswordFlow()
                       .AllowRefreshTokenFlow()
                       .AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange();

                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough();

                options.AddEventHandler<HandleAuthorizationRequestContext>(builder =>
                    builder.UseInlineHandler(async context =>
                    {
                        var provider = context.HttpContext.Request.Query["provider"].ToString();
                        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
                        {
                            if (string.Equals(provider, "google", StringComparison.OrdinalIgnoreCase))
                            {
                                await context.HttpContext.ChallengeAsync("Google");
                            }
                            else if (string.Equals(provider, "facebook", StringComparison.OrdinalIgnoreCase))
                            {
                                await context.HttpContext.ChallengeAsync("Facebook");
                            }
                            context.HandleRequest();
                        }
                    }));
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

