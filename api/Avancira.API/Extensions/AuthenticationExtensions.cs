using Microsoft.Extensions.DependencyInjection;
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
    /// CHANGE 1: This is actually CORRECT for the API project
    /// The API should ONLY validate tokens, not issue them
    /// </summary>
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // CHANGE 2: API uses OpenIddict validation to validate access tokens
        // This validates tokens issued by the Auth server's OpenIddict
        services.AddAuthentication(options =>
        {
            // IMPORTANT: API uses validation scheme, NOT server scheme
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// CHANGE 3: NEW METHOD - Configure OpenIddict validation for API
    /// This must be called in the API's Program.cs/Startup.cs
    /// </summary>
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
            .AddValidation(options =>
            {
                // CHANGE 4: Configure the issuer (your Auth server)
                // This tells the API where to validate tokens from
                options.SetIssuer(authServerUrl);

                // CHANGE 5: Use introspection if Auth and API are separate
                // Comment this out if they share the same database
                // options.UseIntrospection()
                //        .SetClientId("api-resource")
                //        .SetClientSecret("your-secret");

                // CHANGE 6: Use local validation (both use same database)
                // This is more efficient than introspection
                options.UseLocalServer();

                // CHANGE 7: Use ASP.NET Core integration
                options.UseAspNetCore();

                // CHANGE 8: Use System.Net.Http for remote validation if needed
                options.UseSystemNetHttp();
            });

        return services;
    }
}