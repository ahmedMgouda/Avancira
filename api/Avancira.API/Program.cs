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

        // ===== STEP 1: Database Configuration =====
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

        // ===== STEP 2: Add OpenIddict VALIDATION (API project only) =====
        // CRITICAL: This must come before AddApiAuthentication
        var authServerUrl = builder.Configuration["Auth:Issuer"];

        if (string.IsNullOrWhiteSpace(authServerUrl))
        {
            throw new InvalidOperationException(
                "Auth:Issuer configuration is required. " +
                "This should point to your Auth server URL (e.g., https://localhost:5001)");
        }

        // Configure OpenIddict validation for the API
        builder.Services.AddApiOpenIddictValidation(authServerUrl);

        // ===== STEP 3: Add API Authentication =====
        // This configures authentication to use OpenIddict validation
        builder.Services.AddApiAuthentication();

        // ===== STEP 4: Add Controllers (API only, no views) =====
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

        // ===== STEP 5: Configure middleware pipeline =====
        // UseAvanciraFramework already includes:
        // - app.UseRouting()
        // - app.UseCors()
        // - app.UseAuthentication()
        // - app.UseAuthorization()
        app.UseAvanciraFramework();

        app.UseHttpsRedirection();

        // ===== STEP 6: Map SignalR hub and controllers =====
        app.MapHub<NotificationHub>(AuthConstants.Endpoints.Notification);
        app.MapControllers();

        app.Run();
    }
}

// Required for integration tests
public partial class Program { }