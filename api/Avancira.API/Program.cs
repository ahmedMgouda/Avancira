using Avancira.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Avancira.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureAvanciraFramework();

builder.Services.AddSignalR();

builder.Services.AddControllers(options =>
{
    // Set the default produces response type to application/json globally
    options.Filters.Add(new ProducesAttribute("application/json"));
});

// Register your dependencies with Aspire

var app = builder.Build();

app.UseAvanciraFramework();


app.UseHttpsRedirection();

app.MapControllers();
app.MapHub<Avancira.API.Hubs.ChatHub>(Avancira.API.Hubs.ChatHub.HubUrl);


app.Run();
