using Avancira.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureAvanciraFramework();



builder.Services.AddControllers();

// Register your dependencies with Aspire

var app = builder.Build();

app.UseAvanciraFramework();


app.UseHttpsRedirection();

app.MapControllers();


app.Run();
