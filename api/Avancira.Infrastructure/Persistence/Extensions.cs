using Avancira.Application.Persistence;
using Avancira.Infrastructure.Common.Extensions;
using Avancira.Infrastructure.Persistence.Interceptors;
using Avancira.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace Avancira.Infrastructure.Persistence;

/// <summary>
/// Provides extension methods for configuring and initializing
/// the database layer of the application.
/// Supports PostgreSQL and SQL Server providers.
/// </summary>
public static class Extensions
{
    private static readonly ILogger _logger = Log.ForContext(typeof(Extensions));

    // ───────────────────────────────────────────────────────────────
    // 1️⃣ ConfigureDatabase: bind DatabaseOptions and register interceptors
    // ───────────────────────────────────────────────────────────────
    public static WebApplicationBuilder ConfigureDatabase(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services
            .AddOptions<DatabaseOptions>()
            .BindConfiguration(nameof(DatabaseOptions))
            .ValidateDataAnnotations()
            .PostConfigure(options =>
            {
                // Expand any environment variables in connection string
                options.ConnectionString = options.ConnectionString.ExpandEnvironmentVariables();
            });

        // Register common EF interceptors (audit, soft delete, etc.)
        builder.Services.AddScoped<ISaveChangesInterceptor, AuditInterceptor>();

        return builder;
    }

    // ───────────────────────────────────────────────────────────────
    // 2️⃣ BindDbContext: actually register DbContext using validated options
    // ───────────────────────────────────────────────────────────────
    public static IServiceCollection BindDbContext<TContext>(
        this IServiceCollection services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<TContext>((sp, options) =>
        {
            var dbConfig = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            ValidateDatabaseOptions(dbConfig);

            ConfigureProvider(options, dbConfig);
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        });

        return services;
    }

    // ───────────────────────────────────────────────────────────────
    // 3️⃣ AddDatabaseSeeders: register all seeding components
    // ───────────────────────────────────────────────────────────────
    public static IServiceCollection AddDatabaseSeeders(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register initializer & seed orchestrator
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<DataSeederOrchestrator>();

       
        services.AddScoped<RoleSeeder>();
        services.AddScoped<UserSeeder>();
        services.AddScoped<CountrySeeder>();
        services.AddScoped<CategorySeeder>();
        services.AddScoped<ListingSeeder>();
        services.AddScoped<ListingCategorySeeder>();
        services.AddScoped<PromoCodeSeeder>();

        return services;
    }

    // ───────────────────────────────────────────────────────────────
    // 4️⃣ InitializeDatabaseAsync: apply migrations & seed
    // ───────────────────────────────────────────────────────────────
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await using var scope = app.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();

        try
        {
            _logger.Information("Initializing database...");
            await initializer.InitializeAsync();
            _logger.Information("Database initialization completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Database initialization failed: {Message}", ex.Message);
            throw;
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 5️⃣ Internal helpers
    // ───────────────────────────────────────────────────────────────
    private static void ConfigureProvider(DbContextOptionsBuilder builder, DatabaseOptions options)
    {
        var provider = options.Provider?.Trim().ToUpperInvariant();
        var connection = options.ConnectionString?.Trim();

        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Database provider or connection string not configured.");

        builder.ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning));

        switch (provider)
        {
            case DbProviders.PostgreSQL:
                builder.UseNpgsql(connection, npgsql =>
                {
                    npgsql.MigrationsAssembly("Avancira.Migrations");
                    npgsql.EnableRetryOnFailure(5);
                });
                break;

            case DbProviders.MSSQL:
                builder.UseSqlServer(connection, sql =>
                {
                    sql.MigrationsAssembly("Avancira.Migrations");
                    sql.EnableRetryOnFailure(5);
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported database provider '{provider}'. Supported providers: postgresql, mssql");
        }

        builder.EnableSensitiveDataLogging();
    }

    private static void ValidateDatabaseOptions(DatabaseOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Provider))
            throw new InvalidOperationException("Missing DatabaseOptions.Provider (expected 'postgresql' or 'mssql').");

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new InvalidOperationException("Missing DatabaseOptions.ConnectionString.");
    }
}

