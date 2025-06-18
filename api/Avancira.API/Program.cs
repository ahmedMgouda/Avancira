using Avancira.Infrastructure;
using Avancira.Infrastructure.Persistence;
using Avancira.ServiceDefaults;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureAvanciraFramework();

// Add PostgreSQL connection from Aspire
builder.AddNpgsqlDbContext<AvanciraDbContext>("avancira", configureDbContextOptions: options =>
{
    options.EnableSensitiveDataLogging();
});

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
