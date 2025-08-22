using Avancira.Application.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Avancira.API;

public static class AuthenticationExtensions
{
    public static AuthenticationBuilder AddExternalAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var google = configuration.GetSection("Avancira:ExternalServices:Google").Get<GoogleOptions>()
            ?? throw new InvalidOperationException("Missing Google OAuth configuration");
        if (string.IsNullOrWhiteSpace(google.ClientId) || string.IsNullOrWhiteSpace(google.ClientSecret))
        {
            throw new InvalidOperationException("Google OAuth ClientId or ClientSecret is missing in configuration");
        }

        var facebook = configuration.GetSection("Avancira:ExternalServices:Facebook").Get<FacebookOptions>()
            ?? throw new InvalidOperationException("Missing Facebook OAuth configuration");
        if (string.IsNullOrWhiteSpace(facebook.AppId) || string.IsNullOrWhiteSpace(facebook.AppSecret))
        {
            throw new InvalidOperationException("Facebook OAuth AppId or AppSecret is missing in configuration");
        }

        return services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddGoogle(o =>
            {
                o.ClientId = google.ClientId;
                o.ClientSecret = google.ClientSecret;
            })
            .AddFacebook(o =>
            {
                o.AppId = facebook.AppId;
                o.AppSecret = facebook.AppSecret;
            });
    }
}

