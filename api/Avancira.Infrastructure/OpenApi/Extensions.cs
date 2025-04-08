using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Avancira.Infrastructure.OpenApi
{
    public static class Extensions
    {
        public static IServiceCollection ConfigureOpenApi(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            // Add API Explorer for Swagger
            services.AddEndpointsApiExplorer();

            // Configure Swagger with JWT Bearer authentication
            services.AddSwaggerGen(options =>
            {
                options.EnableAnnotations();

                options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT Authorization header using the Bearer scheme."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" }
                        },
                        Array.Empty<string>()
                    }
                });

            });

            return services;
        }

        public static WebApplication UseOpenApi(this WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);

            // Only use Swagger UI in Development or Docker environments
            if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "docker")
            {
                app.UseSwagger(); // Enable Swagger
                app.UseSwaggerUI(options =>
                {
                    //options.DocExpansion(DocExpansion.None); // No expansion by default
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1"); // Define Swagger endpoint for v1
                    options.DisplayRequestDuration(); // Display the request duration in Swagger UI
                });
            }

            return app;
        }
    }
}
