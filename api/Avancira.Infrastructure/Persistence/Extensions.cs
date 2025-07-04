﻿using Avancira.Application.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
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

        try
        {
            // Get the DbContext and check if there are any pending migrations
            var context = serviceScope.ServiceProvider.GetRequiredService<AvanciraDbContext>();

            // Ensure database exists and apply migrations
            context.Database.EnsureCreated();

            // Check if there are pending migrations
            if (context.Database.GetPendingMigrations().Any())
            {
                // Apply pending migrations
                context.Database.Migrate();
                Logger.Information("Applied pending migrations.");
            }
            else
            {
                Logger.Information("No pending migrations.");
            }

            // Optionally, seed the database if needed
            var initializers = serviceScope.ServiceProvider.GetServices<IDbInitializer>();
            foreach (var initializer in initializers)
            {
                initializer.SeedAsync(CancellationToken.None).Wait();
            }
        }
        catch (Exception ex)
        {
            // If database setup fails (e.g., in Aspire mode where it's handled elsewhere), log and continue
            Logger.Warning("Database setup failed, this might be expected in Aspire mode: {Error}", ex.Message);
        }

        return app;
    }
}
