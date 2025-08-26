using Avancira.Infrastructure.Messaging;
using Avancira.Infrastructure;
using Avancira.Infrastructure.Persistence;
using Avancira.ServiceDefaults;
using Avancira.API;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Server;
using OpenIddict.Server.Events;
using OpenIddict.Validation.AspNetCore;

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
var authLogger = authLoggerFactory.CreateLogger("Authentication");

var googleSection = builder.Configuration.GetSection("Avancira:ExternalServices:Google");
var facebookSection = builder.Configuration.GetSection("Avancira:ExternalServices:Facebook");

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

if (!string.IsNullOrWhiteSpace(googleSection["ClientId"]) && !string.IsNullOrWhiteSpace(googleSection["ClientSecret"]))
{
    authBuilder.AddGoogle(o =>
    {
        o.ClientId = googleSection["ClientId"]!;
        o.ClientSecret = googleSection["ClientSecret"]!;
    });
}
else
{
    authLogger.LogWarning("Google OAuth configuration is missing or incomplete. Google authentication will not be available.");
}

if (!string.IsNullOrWhiteSpace(facebookSection["AppId"]) && !string.IsNullOrWhiteSpace(facebookSection["AppSecret"]))
{
    authBuilder.AddFacebook(o =>
    {
        o.AppId = facebookSection["AppId"]!;
        o.AppSecret = facebookSection["AppSecret"]!;
    });
}
else
{
    authLogger.LogWarning("Facebook OAuth configuration is missing or incomplete. Facebook authentication will not be available.");
}

builder.Services.AddOpenIddict()
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token");

        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange();

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough();

        options.AddEventHandler<HandleAuthorizationRequestContext>(builder =>
            builder.UseInlineHandler(async context =>
            {
                var provider = context.HttpContext.Request.Query["provider"].ToString();
                if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
                {
                    if (string.Equals(provider, "google", StringComparison.OrdinalIgnoreCase))
                    {
                        await context.HttpContext.ChallengeAsync("Google");
                    }
                    else if (string.Equals(provider, "facebook", StringComparison.OrdinalIgnoreCase))
                    {
                        await context.HttpContext.ChallengeAsync("Facebook");
                    }
                    context.HandleRequest();
                }
            }));
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAvanciraFramework();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notification");

app.MapControllers();


app.Run();

public partial class Program { }
