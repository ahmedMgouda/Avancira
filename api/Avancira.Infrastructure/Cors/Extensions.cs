using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Avancira.Infrastructure.Cors;
public static class Extensions
{
    private const string CorsPolicy = nameof(CorsPolicy);
    internal static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        var corsOptions = config.GetSection(nameof(CorsOptions)).Get<CorsOptions>();
        if (corsOptions == null) { return services; }
        
        return services.AddCors(opt =>
        opt.AddPolicy(CorsPolicy, policy =>
        {
            policy.AllowAnyHeader()
                  .AllowAnyMethod();
            
            // For SignalR to work with authentication, we need to allow credentials
            // and specify exact origins (not wildcard)
            if (corsOptions.AllowedOrigins.Any())
            {
                policy.AllowCredentials()
                      .WithOrigins(corsOptions.AllowedOrigins.ToArray());
            }
            else
            {
                // Fallback for development - allow common development origins
                policy.AllowCredentials()
                      .WithOrigins(
                          "https://localhost:4200",
                          "http://localhost:4200",
                          "https://10.5.0.2:4200",
                          "http://10.5.0.2:4200",
                          "https://localhost:3000",
                          "http://localhost:3000"
                      );
            }
        }));
    }

    internal static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app)
    {
        return app.UseCors(CorsPolicy);
    }
}
