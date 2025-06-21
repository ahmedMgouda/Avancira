using Avancira.Infrastructure;
using Avancira.Infrastructure.Persistence;
using Avancira.ServiceDefaults;
using Microsoft.AspNetCore.Mvc;

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
});

// Register your dependencies with Aspire

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAvanciraFramework();


app.UseHttpsRedirection();

app.MapControllers();


app.Run();
