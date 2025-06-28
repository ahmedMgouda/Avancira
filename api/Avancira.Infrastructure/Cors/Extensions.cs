using Avancira.Infrastructure.Cors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class Extensions
{
    private const string CorsPolicyName = nameof(CorsPolicyName);

    internal static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        var corsOptions = config
            .GetSection(nameof(CorsOptions))
            .Get<CorsOptions>();

        if (corsOptions == null)
        {
            throw new InvalidOperationException("Missing CORS configuration in app settings.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy.AllowAnyMethod()
                      .AllowAnyHeader();

                if (env.IsDevelopment())
                {
                    policy.SetIsOriginAllowed(_ => true)
                          .AllowCredentials();
                }
                else
                {
                    if (corsOptions.AllowedOrigins?.Any() != true)
                    {
                        throw new InvalidOperationException(
                            "CORS is enabled but no AllowedOrigins are configured for production.");
                    }

                    // Use only configured origins in production with credentials
                    policy.WithOrigins(corsOptions.AllowedOrigins.ToArray())
                          .AllowCredentials();
                }
            });
        });

        return services;
    }

    internal static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app)
    {
        return app.UseCors(CorsPolicyName);
    }
}

