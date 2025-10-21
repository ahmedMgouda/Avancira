using Avancira.Auth.Extensions;
using Avancira.Auth.OpenIddict;
using Avancira.Infrastructure.Composition;
using Avancira.Infrastructure.Identity;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddAvanciraInfrastructure();

        // Database binding (works for Aspire and non-Aspire)
        var isAspire = builder.Configuration.GetConnectionString("avancira") is not null ||
                       builder.Environment.IsDevelopment();

        if (isAspire)
        {
            builder.AddNpgsqlDbContext<AvanciraDbContext>("avancira", configureDbContextOptions: opts =>
            {
                opts.EnableSensitiveDataLogging();
            });
        }
        else
        {
            builder.Services.BindDbContext<AvanciraDbContext>();
        }

      

        builder.Services.AddAuthServerAuthentication();
        builder.Services.AddInfrastructureIdentity(builder.Configuration);

        // External providers (Google, Facebook, etc.)
        using var authLoggerFactory = LoggerFactory.Create(cfg => cfg.AddConsole());
        var authLogger = authLoggerFactory.CreateLogger("ExternalAuth");
        builder.Services.AddExternalAuthentication(builder.Configuration, authLogger);

        builder.Services.AddControllersWithViews()
            .AddJsonOptions(opt =>
            {
                opt.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

    
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<OpenIddictClientSeeder>();

  
        var app = builder.Build();


        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseCorsPolicy();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        try
        {
            await app.InitializeDatabaseAsync();

            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<OpenIddictClientSeeder>();
            await seeder.SeedAsync();

            app.Logger.LogInformation("Database and OpenIddict seeding completed successfully.");
        }
        catch (Exception ex)
        {
            app.Logger.LogCritical(ex, "Database initialization failed: {Error}", ex.Message);
            throw;
        }

        // ═════════════════════════════════════════════════════════
        // 7. STARTUP LOGGING
        // ═════════════════════════════════════════════════════════
        app.Logger.LogInformation(" Avancira Auth Server Started");
        app.Logger.LogInformation(" Environment: {Env}", app.Environment.EnvironmentName);
        app.Logger.LogInformation(" OpenIddict Authority: {Issuer}", builder.Configuration["Auth:Issuer"]);

        await app.RunAsync();
    }
}

public partial class Program { }
