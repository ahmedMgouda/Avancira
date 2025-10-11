using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Avancira.Auth.Extensions;

public static class AuthenticationExtensions
{
    private const string ApplicationCookieName = ".Avancira.Identity";
    private const string ExternalCookieName = ".Avancira.Identity.External";

    private static readonly TimeSpan ApplicationCookieLifetime = TimeSpan.FromHours(2);
    private static readonly TimeSpan ExternalCookieLifetime = TimeSpan.FromMinutes(15);

    public static IServiceCollection AddAuthServerAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
            options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        });

        services.AddAuthorization();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = ApplicationCookieName;
            options.LoginPath = "/account/login";
            options.LogoutPath = "/account/logout";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = ApplicationCookieLifetime;
        });

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

    public static AuthenticationBuilder AddExternalAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Do NOT set global defaults here; Program.cs already does that.
        var builder = new AuthenticationBuilder(services);

        // ---- Google ----
        var googleSection = configuration.GetSection("Avancira:ExternalServices:Google");
        var google = googleSection.Get<GoogleOptions>();
        if (!string.IsNullOrWhiteSpace(google?.ClientId) && !string.IsNullOrWhiteSpace(google.ClientSecret))
        {
            builder.AddGoogle(GoogleDefaults.AuthenticationScheme, o =>
            {
                // Bind everything, including CallbackPath if present in appsettings.
                googleSection.Bind(o);

                o.SignInScheme = IdentityConstants.ExternalScheme;
                o.SaveTokens = true;

                // Ensure email scope is requested (usually default, but explicit is safer).
                if (!o.Scope.Contains("email"))
                {
                    o.Scope.Add("email");
                }

                if (!o.Scope.Contains("profile"))
                {
                    o.Scope.Add("profile");
                }
            });
        }
        else
        {
            logger.LogWarning("Google OAuth configuration is missing or incomplete. Google authentication will not be available.");
        }

        // ---- Facebook ----
        var facebookSection = configuration.GetSection("Avancira:ExternalServices:Facebook");
        var facebook = facebookSection.Get<FacebookOptions>();
        if (!string.IsNullOrWhiteSpace(facebook?.AppId) && !string.IsNullOrWhiteSpace(facebook.AppSecret))
        {
            builder.AddFacebook(FacebookDefaults.AuthenticationScheme, o =>
            {
                facebookSection.Bind(o);

                o.SignInScheme = IdentityConstants.ExternalScheme;
                o.SaveTokens = true;

                // Facebook often requires explicit fields + scope to get email.
                if (!o.Scope.Contains("email"))
                {
                    o.Scope.Add("email");
                }

                o.Fields.Add("email");
                o.Fields.Add("name");

                // Map email -> ClaimTypes.Email if not already mapped.
                o.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
                o.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
            });
        }
        else
        {
            logger.LogWarning("Facebook OAuth configuration is missing or incomplete. Facebook authentication will not be available.");
        }

        return builder;
    }
}
