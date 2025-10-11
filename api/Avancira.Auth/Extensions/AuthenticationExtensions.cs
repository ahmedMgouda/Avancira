using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Avancira.Auth.Extensions;

/// <summary>
/// Authentication configuration for the Auth MVC project
/// Handles both Identity cookies and external providers (Google, Facebook)
/// </summary>
public static class AuthenticationExtensions
{
    private const string ApplicationCookieName = ".Avancira.Identity";
    private const string ExternalCookieName = ".Avancira.Identity.External";

    private static readonly TimeSpan ApplicationCookieLifetime = TimeSpan.FromHours(2);
    private static readonly TimeSpan ExternalCookieLifetime = TimeSpan.FromMinutes(15);

    /// <summary>
    /// CHANGE 1: Renamed from AddAuthServerAuthentication to be more descriptive
    /// Configures Identity cookie authentication for the Auth server
    /// </summary>
    public static IServiceCollection AddAuthServerAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // CHANGE 2: Removed redundant AddAuthentication call
        // Identity.AddIdentity() already registers authentication services
        // We only need to configure the cookies here

        services.AddAuthorization();

        // Configure the main application cookie (used after login)
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = ApplicationCookieName;
            options.LoginPath = "/account/login";
            options.LogoutPath = "/account/logout";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

            // CHANGE 3: SameSite.Lax is correct for auth server
            // Allows the cookie to be sent with top-level navigations
            options.Cookie.SameSite = SameSiteMode.Lax;

            options.SlidingExpiration = true;
            options.ExpireTimeSpan = ApplicationCookieLifetime;

            // CHANGE 4: Added access denied path
            options.AccessDeniedPath = "/account/access-denied";
        });

        // Configure external authentication cookie (temporary during external login)
        services.ConfigureExternalCookie(options =>
        {
            options.Cookie.Name = ExternalCookieName;
            options.ExpireTimeSpan = ExternalCookieLifetime;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        return services;
    }

    /// <summary>
    /// CHANGE 5: Fixed the AuthenticationBuilder initialization
    /// Original code created a new AuthenticationBuilder which doesn't work properly
    /// </summary>
    public static AuthenticationBuilder AddExternalAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // CHANGE 6: Get the existing AuthenticationBuilder instead of creating new one
        // This ensures external providers are added to the same authentication system
        var builder = services.AddAuthentication();

        // ---- Google Configuration ----
        var googleSection = configuration.GetSection("Avancira:ExternalServices:Google");
        var google = googleSection.Get<GoogleOptions>();

        if (!string.IsNullOrWhiteSpace(google?.ClientId) &&
            !string.IsNullOrWhiteSpace(google.ClientSecret))
        {
            builder.AddGoogle(GoogleDefaults.AuthenticationScheme, o =>
            {
                // Bind all configuration from appsettings
                googleSection.Bind(o);

                // CHANGE 7: Explicitly set SignInScheme
                // This tells Google where to store the external login info
                o.SignInScheme = IdentityConstants.ExternalScheme;
                o.SaveTokens = true;

                // CHANGE 8: Ensure required scopes are present
                if (!o.Scope.Contains("email"))
                {
                    o.Scope.Add("email");
                }

                if (!o.Scope.Contains("profile"))
                {
                    o.Scope.Add("profile");
                }

                // CHANGE 9: Add claim mapping for better compatibility
                o.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
                o.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                o.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
                o.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
            });

            logger.LogInformation("Google authentication configured successfully");
        }
        else
        {
            logger.LogWarning(
                "Google OAuth configuration is missing or incomplete. " +
                "Google authentication will not be available.");
        }

        // ---- Facebook Configuration ----
        var facebookSection = configuration.GetSection("Avancira:ExternalServices:Facebook");
        var facebook = facebookSection.Get<FacebookOptions>();

        if (!string.IsNullOrWhiteSpace(facebook?.AppId) &&
            !string.IsNullOrWhiteSpace(facebook.AppSecret))
        {
            builder.AddFacebook(FacebookDefaults.AuthenticationScheme, o =>
            {
                facebookSection.Bind(o);

                o.SignInScheme = IdentityConstants.ExternalScheme;
                o.SaveTokens = true;

                // CHANGE 10: Facebook requires explicit email scope and field
                if (!o.Scope.Contains("email"))
                {
                    o.Scope.Add("email");
                }

                // CHANGE 11: Request specific fields from Facebook Graph API
                o.Fields.Add("email");
                o.Fields.Add("name");
                o.Fields.Add("first_name");
                o.Fields.Add("last_name");

                // CHANGE 12: Map Facebook claims to standard claim types
                o.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
                o.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                o.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "first_name");
                o.ClaimActions.MapJsonKey(ClaimTypes.Surname, "last_name");
            });

            logger.LogInformation("Facebook authentication configured successfully");
        }
        else
        {
            logger.LogWarning(
                "Facebook OAuth configuration is missing or incomplete. " +
                "Facebook authentication will not be available.");
        }

        return builder;
    }
}