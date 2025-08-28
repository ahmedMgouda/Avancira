using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Avancira.API;

public static class AuthenticationExtensions
{
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
                if (!o.Scope.Contains("email")) o.Scope.Add("email");
                if (!o.Scope.Contains("profile")) o.Scope.Add("profile");
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
                if (!o.Scope.Contains("email")) o.Scope.Add("email");
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
