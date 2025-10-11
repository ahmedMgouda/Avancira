using Avancira.API.Extensions;
using Avancira.Infrastructure;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Messaging;
using Avancira.Infrastructure.Persistence;
using Avancira.ServiceDefaults;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var isUsingAspire = builder.Configuration.GetConnectionString("avancira") != null ||
                   builder.Environment.IsDevelopment();

        if (isUsingAspire)
        {
            // Aspire mode - let Aspire handle database configuration
            builder.ConfigureAvanciraFramework();
            builder.AddNpgsqlDbContext<AvanciraDbContext>("avancira", configureDbContextOptions: options =>
            {
                options.EnableSensitiveDataLogging();
            });
        }
        else
        {
            // Production mode - use traditional database configuration
            builder.ConfigureAvanciraFramework();
            builder.Services.BindDbContext<AvanciraDbContext>();
        }

        var authServerUrl = builder.Configuration["Auth:Issuer"];

        if (string.IsNullOrWhiteSpace(authServerUrl))
        {
            throw new InvalidOperationException(
                "Auth:Issuer configuration is required. " +
                "This should point to your Auth server URL (e.g., https://localhost:9100)");
        }

        builder.Services.AddApiOpenIddictValidation(authServerUrl);

        builder.Services.AddApiAuthentication();

        builder.Services.AddControllers(options =>
        {
            options.Filters.Add(new ProducesAttribute("application/json"));
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

        var app = builder.Build();

        app.UseAvanciraFramework(runDatabasePreparation: true);

        app.UseHttpsRedirection();

        app.MapHub<NotificationHub>(AuthConstants.Endpoints.Notification);
        app.MapControllers();

        app.Run();
    }
}

// Required for integration tests
public partial class Program { }