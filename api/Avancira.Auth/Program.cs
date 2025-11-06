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
        // 1️⃣ Add Infrastructure
        // ============================================================
        builder.AddAvanciraInfrastructure();

        // ============================================================
        // 2️⃣ Configure Database
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
        // 4️⃣ External Providers (Google, Facebook)
        // ============================================================
        using var authLoggerFactory = LoggerFactory.Create(cfg => cfg.AddConsole());
        var authLogger = authLoggerFactory.CreateLogger("ExternalAuth");

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

        // FluentValidation
        builder.Services.AddValidatorsFromAssemblyContaining<RegisterViewModelValidator>();
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddFluentValidationClientsideAdapters();

        builder.Services.AddHealthChecks();

        var app = builder.Build();

        // ============================================================
        // 6️⃣ Run Database Initialization
        // ============================================================
        using (var scope = app.Services.CreateScope())
        {
            var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
            await initializer.InitializeAsync();
        }

        // ============================================================
        // 7️⃣ Middleware Pipeline - ✅ CORRECT ORDER
        // ============================================================
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        // ✅ CRITICAL: UseRouting MUST come before Authentication
        app.UseRouting();

        // ✅ Authentication middleware order is critical
        app.UseAuthentication();
        app.UseAuthorization();

        // ✅ Map controllers AFTER authentication
        app.MapControllers();
        app.MapHealthChecks("/health");
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        // ============================================================
        // 8️⃣ Logging Info
        // ============================================================
        app.Logger.LogInformation("✅ Avancira Auth Server Started");
        app.Logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);
        app.Logger.LogInformation("OpenIddict Authority: {Issuer}", builder.Configuration["Auth:Issuer"]);

        await app.RunAsync();
    }
}

public partial class Program { }