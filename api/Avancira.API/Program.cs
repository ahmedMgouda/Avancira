using Avancira.Infrastructure.Messaging;
using Avancira.Infrastructure;
using Avancira.Infrastructure.Persistence;
using Avancira.ServiceDefaults;
using Avancira.API;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avancira.Infrastructure.Identity;
using Avancira.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var isUsingAspire = builder.Configuration.GetConnectionString("avancira") != null
                    || builder.Environment.IsDevelopment();

if (isUsingAspire)
{
    builder.ConfigureAvanciraFramework();
    builder.AddNpgsqlDbContext<AvanciraDbContext>("avancira", configureDbContextOptions: options =>
    {
        options.EnableSensitiveDataLogging();
    });
}
else
{
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

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    })
    .AddCookie(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ExternalScheme);

using var authLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());

var authLogger = authLoggerFactory.CreateLogger("AuthenticationExtensions");
builder.Services.AddExternalAuthentication(builder.Configuration, authLogger);

builder.Services.AddInfrastructureIdentity(builder.Configuration);

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

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAvanciraFramework();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>(AuthConstants.Endpoints.Notification);

app.MapControllers();
app.MapRazorPages();

app.Run();

public partial class Program { }
