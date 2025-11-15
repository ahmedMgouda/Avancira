using Avancira.Infrastructure.Storage;
using Avancira.Infrastructure.Health;
using Avancira.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avancira.Infrastructure;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ADD INFRASTRUCTURE SERVICES
        builder.AddAvanciraInfrastructure();

        // DATABASE CONFIGURATION
        var isAspire = builder.Configuration.GetConnectionString("avancira") is not null ||
                       builder.Environment.IsDevelopment();

        if (isAspire)
        {
            builder.AddNpgsqlDbContext<AvanciraDbContext>("avancira", configureDbContextOptions: opts =>
            {
                opts.EnableSensitiveDataLogging();
            });
        }
        else
        {
            builder.Services.BindDbContext<AvanciraDbContext>();
        }

        // AUTHENTICATION & AUTHORIZATION
        var authIssuer = builder.Configuration["Auth:Issuer"]
            ?? throw new InvalidOperationException("Missing 'Auth:Issuer' configuration.");

        builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        builder.Services.AddAuthorization();

        builder.Services.AddOpenIddict()
            .AddValidation(options =>
            {
                options.SetIssuer(authIssuer);
                options.UseIntrospection()
                       .SetClientId("resource_server")
                       .SetClientSecret("846B62D0-DEF9-4215-A99D-86E6B8DAB342");

                options.UseSystemNetHttp();
                options.UseAspNetCore();

                if (builder.Environment.IsDevelopment())
                {
                    options.AddEventHandler<
                        OpenIddict.Validation.OpenIddictValidationEvents.ProcessAuthenticationContext>(
                        handler => handler.UseInlineHandler(context =>
                        {
                            if (context.AccessTokenPrincipal is not null)
                            {
                                Console.WriteLine("Token validated successfully");
                                Console.WriteLine($"Subject: {context.AccessTokenPrincipal.FindFirst("sub")?.Value}");
                            }
                            return default;
                        }));
                }
            });

        // CONTROLLERS & JSON CONFIGURATION
        builder.Services.AddControllers(options =>
        {
            options.Filters.Add(new ProducesAttribute("application/json"));
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                JsonIgnoreCondition.WhenWritingNull;
        });

        // HEALTH CHECKS
        builder.Services.AddAvanciraHealthChecks<AvanciraDbContext>(builder.Configuration);

        var app = builder.Build();

        // MIDDLEWARE PIPELINE

        // CORS
        app.UseCorsPolicy();

        // Static files - Default ASP.NET Core wwwroot
        app.UseStaticFiles();

        app.UseFileStorage(app.Configuration);

        // HTTPS & Routing
        app.UseHttpsRedirection();
        app.UseRouting();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Controllers
        app.MapControllers();

        // Health checks
        app.MapAvanciraHealthChecks();

        // STARTUP LOGGING
        app.Logger.LogInformation("🚀 Avancira API Started");
        app.Logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);
        app.Logger.LogInformation("Auth Issuer: {Issuer}", authIssuer);
        app.Logger.LogInformation("Health: /health/live, /health/ready, /health");
        app.Logger.LogInformation("File Storage: /api/files/*");
        app.Logger.LogInformation("Endpoints: /api/*");

        await app.RunAsync();
    }
}

public partial class Program { }