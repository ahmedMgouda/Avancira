using Microsoft.AspNetCore.Authentication;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace Avancira.API;

public static class AuthenticationExtensions
{
    public static AuthenticationBuilder AddExternalAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        var builder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;
        });

        var google = configuration.GetSection("Avancira:ExternalServices:Google").Get<GoogleOptions>();
        if (!string.IsNullOrWhiteSpace(google?.ClientId) && !string.IsNullOrWhiteSpace(google.ClientSecret))
        {
            builder.AddGoogle(o =>
            {
                o.ClientId = google.ClientId;
                o.ClientSecret = google.ClientSecret;
                o.SignInScheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;
            });
        }
        else
        {
            logger.LogWarning("Google OAuth configuration is missing or incomplete. Google authentication will not be available.");
        }

        var facebook = configuration.GetSection("Avancira:ExternalServices:Facebook").Get<FacebookOptions>();
        if (!string.IsNullOrWhiteSpace(facebook?.AppId) && !string.IsNullOrWhiteSpace(facebook.AppSecret))
        {
            builder.AddFacebook(o =>
            {
                o.AppId = facebook.AppId;
                o.AppSecret = facebook.AppSecret;
                o.SignInScheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;
            });
        }
        else
        {
            logger.LogWarning("Facebook OAuth configuration is missing or incomplete. Facebook authentication will not be available.");
        }

        return builder;
    }
}

