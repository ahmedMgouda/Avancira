using Avancira.Auth.Extensions;
using Avancira.Auth.OpenIddict;
using Avancira.Auth.Validators;
using Avancira.Infrastructure.Composition;
using Avancira.Infrastructure.Identity;
using Avancira.Infrastructure.Persistence;
using Avancira.Infrastructure.Persistence.Seeders;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

public partial class Program
{
    public static async Task Main(string[] args)
    {


        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);


        var builder = WebApplication.CreateBuilder(args);

        // ============================================================
        // 1️⃣ Add Infrastructure (no Hangfire started yet)
        // ============================================================
        builder.AddAvanciraInfrastructure();

        // ============================================================
        // 2️⃣ Configure Database (local or Aspire mode)
        // ============================================================
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

        // ============================================================
        // 3️⃣ Authentication & Identity
        // ============================================================
        builder.Services.AddAuthServerAuthentication(builder.Environment);
        builder.Services.AddInfrastructureIdentity(builder.Configuration);
        builder.Services.AddScoped<OpenIddictClientSeeder>();

        // ============================================================
        // 4️⃣ External Providers (Google, Facebook, etc.)
        // ============================================================
        using var authLoggerFactory = LoggerFactory.Create(cfg => cfg.AddConsole());
        var authLogger = authLoggerFactory.CreateLogger("ExternalAuth");

        // FIX: Pass the environment to configure OAuth cookie security properly
        builder.Services.AddExternalAuthentication(
            builder.Configuration,
            authLogger,
            builder.Environment);


        // ============================================================
        // 5️⃣ MVC / JSON
        // ============================================================
        builder.Services.AddControllersWithViews()
            .AddJsonOptions(opt =>
            {
                opt.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });


        builder.Services.AddMemoryCache();

        // Add FluentValidation
        builder.Services.AddValidatorsFromAssemblyContaining<RegisterViewModelValidator>();
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddFluentValidationClientsideAdapters();

        var app = builder.Build();

        // ============================================================
        // 6️⃣ Run Database Initialization FIRST (migrations + seeding)
        // ============================================================
        using (var scope = app.Services.CreateScope())
        {
            var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
            await initializer.InitializeAsync();
        }

        // ============================================================
        // 8️⃣ Middleware Pipeline
        // ============================================================
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCorsPolicy();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        // ============================================================
        // 9️⃣ Logging Info
        // ============================================================
        app.Logger.LogInformation("✅ Avancira Auth Server Started");
        app.Logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);
        app.Logger.LogInformation("OpenIddict Authority: {Issuer}", builder.Configuration["Auth:Issuer"]);

        await app.RunAsync();
    }
}

public partial class Program { }
