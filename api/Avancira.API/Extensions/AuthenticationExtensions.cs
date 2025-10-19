using Avancira.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Validation;
using OpenIddict.Abstractions;

namespace Avancira.API.Extensions;

/// <summary>
/// Authentication configuration for the API project.
/// This project validates access tokens issued by the Auth server.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddApiOpenIddictValidation(
        this IServiceCollection services,
        string authServerUrl)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(authServerUrl))
        {
            throw new ArgumentException(
                "Auth server URL is required for OpenIddict validation",
                nameof(authServerUrl));
        }

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<AvanciraDbContext>();
            })
            .AddValidation(options =>
            {
                options.SetIssuer(authServerUrl);

                // Use introspection to validate encrypted tokens
                options.UseIntrospection()
                       .SetClientId("bff-client")
                       .SetClientSecret("dev-bff-secret");


                options.UseSystemNetHttp();
                options.UseAspNetCore();

                // Debug logging
                options.AddEventHandler<OpenIddict.Validation.OpenIddictValidationEvents.ProcessAuthenticationContext>(
                    handler => handler.UseInlineHandler(context =>
                    {
                        if (context.AccessTokenPrincipal is not null)
                        {
                            Console.WriteLine("✅ TOKEN VALIDATED");
                            Console.WriteLine($"   Subject: {context.AccessTokenPrincipal.FindFirst("sub")?.Value}");
                            Console.WriteLine($"   Scopes: {context.AccessTokenPrincipal.FindFirst("scope")?.Value}");
                        }
                        else
                        {
                            Console.WriteLine("❌ TOKEN VALIDATION FAILED");
                            Console.WriteLine($"   Error: {context.Error}");
                            Console.WriteLine($"   Description: {context.ErrorDescription}");
                        }
                        return default;
                    }));

            });

        return services;
    }
}
