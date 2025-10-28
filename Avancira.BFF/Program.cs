using Avancira.BFF.Configuration;
using Avancira.BFF.Extensions;
using SendGrid.Helpers.Mail;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddBffServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure middleware pipeline
app.UseBffMiddleware(builder.Environment);

// Map endpoints
app.MapBffEndpoints(builder.Environment);

// Startup logging
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var settings = app.Services.GetRequiredService<BffSettings>();

logger.LogInformation("════════════════════════════════════════════════════════");
logger.LogInformation("🚀 Avancira BFF Service Started");
logger.LogInformation("════════════════════════════════════════════════════════");
logger.LogInformation("📍 Environment: {Env}", app.Environment.EnvironmentName);
logger.LogInformation("🔐 Authority: {Authority}", settings.Auth.Authority);
logger.LogInformation("🎯 API Target: {ApiUrl}", settings.ApiBaseUrl);
logger.LogInformation("🍪 Cookie: {Cookie} (Essential claims: {Claims})",
    settings.Cookie.Name,
    string.Join(", ", settings.EssentialClaims));
logger.LogInformation("💾 Token Storage: {Storage}",
    settings.HasRedis ? "Redis (Production)" : "Memory (Development)");
logger.LogInformation("📊 Expected Cookie Size: ~350-450 bytes");
logger.LogInformation("════════════════════════════════════════════════════════");

app.Run();

public partial class Program { }