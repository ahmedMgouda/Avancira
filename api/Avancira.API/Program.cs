using Avancira.Infrastructure.Messaging;
using Avancira.Infrastructure;
using Avancira.Infrastructure.Persistence;
using Avancira.ServiceDefaults;
using Avancira.API;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avancira.Infrastructure.Auth;
using OpenIddict.Validation.AspNetCore;
using Microsoft.AspNetCore.Identity;

public partial class Program {
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

        builder.Services.AddControllers(options =>
        {
            options.Filters.Add(new ProducesAttribute("application/json"));
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

        builder.Services.AddSignalR();
        builder.Services.AddRazorPages();
        builder.Services.AddHttpContextAccessor();

        using var authLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());

        var authLogger = authLoggerFactory.CreateLogger("AuthenticationExtensions");
        builder.Services.AddExternalAuthentication(builder.Configuration, authLogger);

        var app = builder.Build();

        app.UseAvanciraFramework();

        app.UseHttpsRedirection();

        app.MapHub<NotificationHub>(AuthConstants.Endpoints.Notification);

        app.MapControllers();
        app.MapRazorPages();

        app.Run();
    }
}

public partial class Program { }
