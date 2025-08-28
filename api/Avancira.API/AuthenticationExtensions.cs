using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Identity;

namespace Avancira.API;

public static class AuthenticationExtensions
{
    public static AuthenticationBuilder AddExternalAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        // Don't set global defaults here; Program.cs already did that.
        var builder = new AuthenticationBuilder(services);

        // Google
        var googleSection = configuration.GetSection("Avancira:ExternalServices:Google");
        var google = googleSection.Get<GoogleOptions>();
        if (!string.IsNullOrWhiteSpace(google?.ClientId) && !string.IsNullOrWhiteSpace(google.ClientSecret))
        {
            builder.AddGoogle(GoogleDefaults.AuthenticationScheme, o =>
            {
                // Bind extra options like CallbackPath if present
                googleSection.Bind(o);
                o.SignInScheme = IdentityConstants.ExternalScheme;
                o.SaveTokens = true;
            });
        }
        else
        {
            logger.LogWarning("Google OAuth configuration is missing or incomplete. Google authentication will not be available.");
        }

        // Facebook
        var facebookSection = configuration.GetSection("Avancira:ExternalServices:Facebook");
        var facebook = facebookSection.Get<FacebookOptions>();
        if (!string.IsNullOrWhiteSpace(facebook?.AppId) && !string.IsNullOrWhiteSpace(facebook.AppSecret))
        {
            builder.AddFacebook(FacebookDefaults.AuthenticationScheme, o =>
            {
                facebookSection.Bind(o);
                o.SignInScheme = IdentityConstants.ExternalScheme;
                o.SaveTokens = true;
            });
        }
        else
        {
            logger.LogWarning("Facebook OAuth configuration is missing or incomplete. Facebook authentication will not be available.");
        }

        return builder;
    }
}
