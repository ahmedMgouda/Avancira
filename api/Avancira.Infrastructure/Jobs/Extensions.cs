using System;
using Avancira.Application.Jobs;
using Avancira.Application.Persistence;
using Avancira.Infrastructure.Persistence;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Avancira.Infrastructure.Common.Extensions;
using Avancira.Domain.Common.Exceptions;

namespace Avancira.Infrastructure.Jobs;

internal static class Extensions
{
    internal static IServiceCollection ConfigureJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var hangfireOptions = configuration.GetSection(nameof(HangfireOptions)).Get<HangfireOptions>() ?? new HangfireOptions();
        var isUsingAspire = configuration.GetConnectionString("avancira") != null;

        if (!hangfireOptions.Enabled)
        {
            services.AddSingleton<IJobService, DisabledJobService>();
            services.AddSingleton<IPaymentJobService, DisabledPaymentJobService>();
            return services;
        }

        services.AddHangfire((provider, config) =>
        {
            ConfigureStorage(config, hangfireOptions, configuration, isUsingAspire);

            config.UseActivator(new AvanciraJobActivator(provider.GetRequiredService<IServiceScopeFactory>()));
            config.UseFilter(new AvanciraJobFilter(provider));
            config.UseFilter(new LogJobFilter());
        });

        if (hangfireOptions.EnableServer)
        {
            services.AddHangfireServer(options =>
            {
                options.Queues = hangfireOptions.Queues?.Length > 0
                    ? hangfireOptions.Queues
                    : new[] { "default" };
                options.WorkerCount = Math.Max(1, hangfireOptions.WorkerCount);
                options.HeartbeatInterval = TimeSpan.FromSeconds(Math.Max(1, hangfireOptions.HeartbeatIntervalSeconds));
                options.SchedulePollingInterval = TimeSpan.FromSeconds(Math.Max(1, hangfireOptions.SchedulePollingIntervalSeconds));
            });

            services.AddHostedService<RecurringJobsService>();
        }

        services.AddTransient<IJobService, HangfireService>();
        services.AddTransient<IPaymentJobService, PaymentJobImplementationService>();
        return services;
    }

    private static void ConfigureStorage(
        IGlobalConfiguration configuration,
        HangfireOptions hangfireOptions,
        IConfiguration appConfiguration,
        bool isUsingAspire)
    {
        var provider = (hangfireOptions.StorageProvider ?? string.Empty).ToUpperInvariant();

        switch (provider)
        {
            case HangfireStorageProviders.Memory:
                configuration.UseMemoryStorage();
                break;
            case DbProviders.PostgreSQL:
                var postgresConnectionString = ResolveConnectionString(hangfireOptions, appConfiguration, isUsingAspire);

                configuration.UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(postgresConnectionString);

                    if (!string.IsNullOrWhiteSpace(hangfireOptions.Schema))
                    {
                        //options.SchemaN = hangfireOptions.Schema;
                    }
                });
                break;
            case DbProviders.MSSQL:
                var sqlServerConnectionString = ResolveConnectionString(hangfireOptions, appConfiguration, isUsingAspire);

                configuration.UseSqlServerStorage(sqlServerConnectionString);
                break;
            default:
                throw new AvanciraException($"hangfire storage provider {hangfireOptions.StorageProvider} is not supported");
        }
    }

    private static string ResolveConnectionString(
        HangfireOptions hangfireOptions,
        IConfiguration appConfiguration,
        bool isUsingAspire)
    {
        if (!string.IsNullOrWhiteSpace(hangfireOptions.StorageConnectionString))
        {
            return hangfireOptions.StorageConnectionString.ExpandEnvironmentVariables();
        }

        if (isUsingAspire)
        {
            var connectionString = appConfiguration.GetConnectionString("avancira");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new AvanciraException("Aspire connection string 'avancira' not found");
            }

            return connectionString;
        }

        var dbOptions = appConfiguration.GetSection(nameof(DatabaseOptions)).Get<DatabaseOptions>() ??
            throw new AvanciraException("database options cannot be null");

        if (string.IsNullOrWhiteSpace(dbOptions.ConnectionString))
        {
            throw new AvanciraException("database connection string cannot be null or empty");
        }

        return dbOptions.ConnectionString.ExpandEnvironmentVariables();
    }

    internal static IApplicationBuilder UseJobDashboard(this IApplicationBuilder app, IConfiguration config)
    {
        var hangfireOptions = config.GetSection(nameof(HangfireOptions)).Get<HangfireOptions>() ?? new HangfireOptions();

        if (!hangfireOptions.Enabled || !hangfireOptions.EnableDashboard)
        {
            return app;
        }

        var dashboardOptions = new DashboardOptions
        {
            AppPath = "/",
            DashboardTitle = "Avancira Jobs Dashboard",
            StatsPollingInterval = 2000,
            Authorization = new[]
            {
                new HangfireCustomBasicAuthenticationFilter
                {
                    User = hangfireOptions.UserName,
                    Pass = hangfireOptions.Password
                }
            }
        };

        return app.UseHangfireDashboard(hangfireOptions.Route, dashboardOptions);
    }
}
