using Avancira.Infrastructure.Persistence;
using OpenIddict.Validation.AspNetCore;

namespace Avancira.API.Extensions;

/// <summary>
/// Authentication configuration for the API project
/// This project validates access tokens issued by the Auth server
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Configures authentication for API endpoints using OpenIddict validation
    /// 
    /// The API should ONLY validate tokens, not issue them
    /// </summary>
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);


        // This validates tokens issued by the Auth server's OpenIddict
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
                // This tells the API where to validate tokens from
                options.SetIssuer(authServerUrl);

                // Use introspection if Auth and API are separate
                // Comment this out if they share the same database
                options.UseIntrospection()
                       .SetClientId("bff-client")
                       .SetClientSecret("dev-bff-secret");

                // Use local validation (both use same database)
                // This is more efficient than introspection
                //options.UseLocalServer();

                // Use HTTP for communication with Auth container
                options.UseSystemNetHttp();

                // Integrate with ASP.NET Core’s authentication system
                options.UseAspNetCore();
            });

        return services;
    }
}