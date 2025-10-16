using Avancira.BFF.Extensions;
using Avancira.Infrastructure;
using Avancira.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// === 1. Infrastructure ===
builder.ConfigureAvanciraFramework();

builder.Services.BindDbContext<AvanciraDbContext>();


// === 2. BFF Services ===
builder.Services.AddBffAuthentication(builder.Configuration);
builder.Services.AddBffTokenManagement(builder.Configuration);
builder.Services.AddBffAuthorization();
builder.Services.AddBffReverseProxy(builder.Configuration);

// === 3. MVC + Diagnostics ===
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseAvanciraFramework(runDatabasePreparation: false);
// === 4. Middleware ===
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();


// === 5. Endpoints ===
app.MapHealthChecks("/health");
app.MapControllers();
app.MapReverseProxy();

// === 6. Logging ===
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("BFF started in {Env}", app.Environment.EnvironmentName);
logger.LogInformation("Authority: {Auth}", builder.Configuration["Auth:Authority"]);
logger.LogInformation("API Target: {Target}",
    builder.Configuration["ReverseProxy:Clusters:api-cluster:Destinations:primary:Address"]);

app.Run();

public partial class Program { }
