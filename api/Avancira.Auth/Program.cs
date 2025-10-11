using Avancira.Auth.Extensions;
using Avancira.Infrastructure;
using Avancira.Infrastructure.Persistence;
using Avancira.ServiceDefaults;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var isUsingAspire = builder.Configuration.GetConnectionString("avancira") != null ||
                   builder.Environment.IsDevelopment();

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

        builder.Services.AddAuthServerAuthentication();

        builder.Services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new ProducesAttribute("text/html"));
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

        builder.Services.AddMemoryCache();

        using var authLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
        var authLogger = authLoggerFactory.CreateLogger("AuthenticationExtensions");
        builder.Services.AddExternalAuthentication(builder.Configuration, authLogger);

        var app = builder.Build();

        app.UseAvanciraFramework();

        app.UseHttpsRedirection();

        app.MapControllers();

        app.Run();
    }
}

public partial class Program { }
