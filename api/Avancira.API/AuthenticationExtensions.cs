using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace Avancira.API;

public static class AuthenticationExtensions
{
    public static AuthenticationBuilder AddExternalAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger<AuthenticationExtensions> logger)
    {
        var builder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;
        });

        var googleSection = configuration.GetSection("Avancira:ExternalServices:Google");
        services.Configure<GoogleOptions>(googleSection);
        var google = googleSection.Get<GoogleOptions>();
        if (!string.IsNullOrWhiteSpace(google?.ClientId) && !string.IsNullOrWhiteSpace(google.ClientSecret))
        {
            builder.AddGoogle(o =>
            {
                o.SignInScheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;
            });
        }
        else
        {
            logger.LogWarning("Google OAuth configuration is missing or incomplete. Google authentication will not be available.");
        }

        var facebookSection = configuration.GetSection("Avancira:ExternalServices:Facebook");
        services.Configure<FacebookOptions>(facebookSection);
        var facebook = facebookSection.Get<FacebookOptions>();
        if (!string.IsNullOrWhiteSpace(facebook?.AppId) && !string.IsNullOrWhiteSpace(facebook.AppSecret))
        {
            builder.AddFacebook(o =>
            {
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

