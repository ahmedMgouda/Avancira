using Avancira.Application.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.Extensions.Options;
using Avancira.Infrastructure.Persistence.Interceptors;
using Avancira.Infrastructure.Common.Extensions;

namespace Avancira.Infrastructure.Persistence;
public static class Extensions
{
    private static readonly ILogger Logger = Log.ForContext(typeof(Extensions));
    internal static DbContextOptionsBuilder ConfigureDatabase(this DbContextOptionsBuilder builder, string dbProvider, string connectionString)
    {
        builder.ConfigureWarnings(warnings => warnings.Log(RelationalEventId.PendingModelChangesWarning));
        return dbProvider.ToUpperInvariant() switch
        {
            DbProviders.PostgreSQL => builder.UseNpgsql(connectionString, e =>
                                 e.MigrationsAssembly("Avancira.Migrations")).EnableSensitiveDataLogging(),
            DbProviders.MSSQL => builder.UseSqlServer(connectionString, e =>
                                e.MigrationsAssembly("Avancira.Migrations")),
            _ => throw new InvalidOperationException($"DB Provider {dbProvider} is not supported."),
        };
    }

    public static WebApplicationBuilder ConfigureDatabase(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddOptions<DatabaseOptions>()
            .BindConfiguration(nameof(DatabaseOptions))
            .ValidateDataAnnotations()
            .PostConfigure(config =>
            {
                Logger.Information("current db provider: {DatabaseProvider}", config.Provider);
                config.ConnectionString = config.ConnectionString.ExpandEnvironmentVariables();
            });
        builder.Services.AddScoped<ISaveChangesInterceptor, AuditInterceptor>();
        return builder;
    }

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

    public static IApplicationBuilder SetupDatabases(this IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        var configuration = serviceScope.ServiceProvider.GetRequiredService<IConfiguration>();
        
        // Aspire-controlled flags with production-safe defaults
        var dropDatabase = configuration.GetValue<bool>("ASPIRE_DROP_DATABASE", false);
        var runSeeding = configuration.GetValue<bool>("ASPIRE_RUN_SEEDING", false);

        try
        {
            var context = serviceScope.ServiceProvider.GetRequiredService<AvanciraDbContext>();

            // Handle database drop/recreation
            if (dropDatabase)
            {
                context.Database.EnsureDeleted();
                Logger.Information("Database dropped");
            }

            // Ensure database exists
            context.Database.EnsureCreated();

            // migrations
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
                Logger.Information("Migrations applied");
            }

            // Always run IdentityDbInitializer for essential roles and admin user
            var identityInitializer = serviceScope.ServiceProvider.GetServices<IDbInitializer>()
                .FirstOrDefault(i => i.GetType() == typeof(IdentityDbInitializer));
            if (identityInitializer != null)
            {
                identityInitializer.SeedAsync(CancellationToken.None).Wait();
                Logger.Information("Identity data seeded (roles and admin user)");
            }

            // Handle application data seeding only if Aspire allows it
            if (runSeeding)
            {
                var avanciraInitializer = serviceScope.ServiceProvider.GetServices<IDbInitializer>()
                    .FirstOrDefault(i => i.GetType() == typeof(AvanciraDbInitializer));
                if (avanciraInitializer != null)
                {
                    avanciraInitializer.SeedAsync(CancellationToken.None).Wait();
                    Logger.Information("Avancira application data seeding completed");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Database setup failed: {Error}", ex.Message);
            throw;
        }

        return app;
    }
}
