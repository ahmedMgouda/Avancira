using Avancira.Application.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Avancira.Infrastructure.Persistence.Interceptors;
using Avancira.Infrastructure.Common.Extensions;
using Avancira.Infrastructure.Persistence.Seeders;
using Avancira.Infrastructure.Identity.Seeders;

namespace Avancira.Infrastructure.Persistence;

/// <summary>
/// Provides extension methods for configuring and initializing the database.
/// Handles provider setup, context binding, migrations, and orchestrated seeding.
/// </summary>
public static class Extensions
{
    private static readonly Serilog.ILogger LogContext = Log.ForContext(typeof(Extensions));

    // ============================================================
    // DATABASE CONFIGURATION
    // ============================================================

    internal static DbContextOptionsBuilder ConfigureDatabase(
        this DbContextOptionsBuilder builder,
        string dbProvider,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureWarnings(warnings =>
            warnings.Log(RelationalEventId.PendingModelChangesWarning));

        return dbProvider.ToUpperInvariant() switch
        {
            DbProviders.PostgreSQL => builder
                .UseNpgsql(connectionString, e => e.MigrationsAssembly("Avancira.Migrations"))
                .EnableSensitiveDataLogging(),
            DbProviders.MSSQL => builder
                .UseSqlServer(connectionString, e => e.MigrationsAssembly("Avancira.Migrations")),
            _ => throw new InvalidOperationException(
                $"Database provider '{dbProvider}' is not supported.")
        };
    }

    public static WebApplicationBuilder ConfigureDatabase(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<DatabaseOptions>()
            .BindConfiguration(nameof(DatabaseOptions))
            .ValidateDataAnnotations()
            .PostConfigure(options =>
            {
                LogContext.Information("Using database provider: {Provider}", options.Provider);
                options.ConnectionString = options.ConnectionString.ExpandEnvironmentVariables();
            });

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditInterceptor>();

        return builder;
    }

    // ============================================================
    // DB CONTEXT REGISTRATION
    // ============================================================

    public static IServiceCollection BindDbContext<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<TContext>((sp, options) =>
        {
            var dbConfig = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.ConfigureDatabase(dbConfig.Provider, dbConfig.ConnectionString);
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        });

        return services;
    }

    // ============================================================
    // SEEDERS REGISTRATION
    // ============================================================

    /// <summary>
    /// Registers all seeders (Identity + Domain) and the orchestrator that runs them in order.
    /// </summary>
    public static IServiceCollection AddDatabaseSeeders(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Orchestrator
        services.AddScoped<DataSeederOrchestrator>();

        // === Identity Seeders ===
        services.AddScoped<RoleSeeder>();
        services.AddScoped<AdminUserSeeder>();
        services.AddScoped<UserSeeder>();
        services.AddScoped<OpenIddictClientSeeder>();

        // === Domain Seeders ===
        services.AddScoped<CountrySeeder>();
        services.AddScoped<CategorySeeder>();
        services.AddScoped<ListingSeeder>();
        services.AddScoped<ListingCategorySeeder>();
        services.AddScoped<PromoCodeSeeder>();

        return services;
    }

    // ============================================================
    // DATABASE INITIALIZATION PIPELINE
    // ============================================================

    /// <summary>
    /// Applies migrations, creates the database if missing, and runs orchestrated seeders.
    /// Controlled by Aspire environment flags.
    /// </summary>
    public static async Task<IApplicationBuilder> SetupDatabasesAsync(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await using var scope = app.ApplicationServices.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var configuration = sp.GetRequiredService<IConfiguration>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DatabaseSetup");

        var dropDatabase = configuration.GetValue("ASPIRE_DROP_DATABASE", false);
        var runSeeding = configuration.GetValue("ASPIRE_RUN_SEEDING", false);

        try
        {
            var context = sp.GetRequiredService<AvanciraDbContext>();
            LogContext.Information("Starting database setup...");

            // === Optional Reset ===
            if (!dropDatabase)
            {
                await context.Database.EnsureDeletedAsync();
                LogContext.Warning("Database dropped (ASPIRE_DROP_DATABASE=true).");
            }

            // === Ensure Creation ===
            await context.Database.EnsureCreatedAsync();

            // === Apply Pending Migrations ===
            var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
            if (pendingMigrations.Count > 0)
            {
                await context.Database.MigrateAsync();
                LogContext.Information("Applied {Count} pending migrations.", pendingMigrations.Count);
            }
            else
            {
                LogContext.Information("No pending migrations detected.");
            }

            // === Run Seeders ===
            if (!runSeeding)
            {
                LogContext.Information("Executing seeders via {Orchestrator}...", nameof(DataSeederOrchestrator));

                var orchestrator = sp.GetRequiredService<DataSeederOrchestrator>();
                await orchestrator.RunAsync(CancellationToken.None);

                LogContext.Information("All seeders executed successfully.");
            }
            else
            {
                LogContext.Information("Seeding skipped (ASPIRE_RUN_SEEDING=false).");
            }

            LogContext.Information("Database setup completed successfully.");
        }
        catch (Exception ex)
        {
            LogContext.Error(ex, "Database setup failed: {Error}", ex.Message);
            throw;
        }

        return app;
    }
}
