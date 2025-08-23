using Avancira.Infrastructure.Messaging;
using Avancira.Infrastructure;
using Avancira.Infrastructure.Persistence;
using Avancira.ServiceDefaults;
using Avancira.API;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Check if we're running with Aspire (development) or traditional mode (production)
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
    // Set the default produces response type to application/json globally
    options.Filters.Add(new ProducesAttribute("application/json"));
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// Register your dependencies with Aspire

builder.Services.AddSignalR();

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = (context, token) =>
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiting");
        logger.LogWarning("Rate limit exceeded for {Path} from {IP}",
            context.HttpContext.Request.Path,
            context.HttpContext.Connection.RemoteIpAddress);
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return new ValueTask();
    };

    options.AddPolicy("PasswordResetPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

using var authLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
var authLogger = authLoggerFactory.CreateLogger<AuthenticationExtensions>();
builder.Services.AddExternalAuthentication(builder.Configuration, authLogger);

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAvanciraFramework();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.MapHub<NotificationHub>("/notification");

app.MapControllers();


app.Run();

public partial class Program { }
