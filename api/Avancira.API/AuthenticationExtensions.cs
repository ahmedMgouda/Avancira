using Avancira.Application.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Avancira.API;

public static class AuthenticationExtensions
{
    public static AuthenticationBuilder AddExternalAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging();

        var builder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        });

        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<AuthenticationExtensions>>();

        var google = configuration.GetSection("Avancira:ExternalServices:Google").Get<GoogleOptions>();
        if (!string.IsNullOrWhiteSpace(google?.ClientId) && !string.IsNullOrWhiteSpace(google.ClientSecret))
        {
            builder.AddGoogle(o =>
            {
                o.ClientId = google.ClientId;
                o.ClientSecret = google.ClientSecret;
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
            });
        }
        else
        {
            logger.LogWarning("Facebook OAuth configuration is missing or incomplete. Facebook authentication will not be available.");
        }

        return builder;
    }
}

