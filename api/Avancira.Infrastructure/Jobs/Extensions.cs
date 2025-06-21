using Avancira.Application.Jobs;
using Avancira.Application.Persistence;
using Avancira.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.SqlServer;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Avancira.Infrastructure.Common.Extensions;
using Avancira.Domain.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Avancira.Infrastructure.Jobs;

namespace Avancira.Infrastructure.Jobs;

internal static class Extensions
{
    internal static IServiceCollection ConfigureJobs(this IServiceCollection services, IConfiguration configuration)
    {
        // Check if we're running with Aspire (development) or traditional mode (production)
        var isUsingAspire = configuration.GetConnectionString("avancira") != null;
        
        services.AddHangfireServer(o =>
        {
            o.HeartbeatInterval = TimeSpan.FromSeconds(30);
            o.Queues = new string[] { "default", "email" };
            o.WorkerCount = 5;
            o.SchedulePollingInterval = TimeSpan.FromSeconds(30);
        });

        if (isUsingAspire)
        {
            // In Aspire mode, use the connection string provided by Aspire
            services.AddHangfire((provider, config) =>
            {
                var connectionString = configuration.GetConnectionString("avancira");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new AvanciraException("Aspire connection string 'avancira' not found");
                }

                config.UsePostgreSqlStorage(o =>
                {
                    o.UseNpgsqlConnection(connectionString);
                });

                config.UseFilter(new AvanciraJobFilter(provider));
                config.UseFilter(new LogJobFilter());
            });
        }
        else
        {
            // Traditional mode - use DatabaseOptions
            var dbOptions = configuration.GetSection(nameof(DatabaseOptions)).Get<DatabaseOptions>() ??
                throw new AvanciraException("database options cannot be null");

            services.AddHangfire((provider, config) =>
            {
                switch (dbOptions.Provider.ToUpperInvariant())
                {
                    case DbProviders.PostgreSQL:
                        config.UsePostgreSqlStorage(o =>
                        {
                            o.UseNpgsqlConnection(dbOptions.ConnectionString.ExpandEnvironmentVariables());
                        });
                        break;

                    case DbProviders.MSSQL:
                        config.UseSqlServerStorage(dbOptions.ConnectionString);
                        break;

                    default:
                        throw new AvanciraException($"hangfire storage provider {dbOptions.Provider} is not supported");
                }

                config.UseFilter(new AvanciraJobFilter(provider));
                config.UseFilter(new LogJobFilter());
            });
        }

        services.AddTransient<IJobService, HangfireService>();
        services.AddTransient<IPaymentJobService, PaymentJobImplementationService>();
        services.AddHostedService<RecurringJobsService>();
        return services;
    }

    internal static IApplicationBuilder UseJobDashboard(this IApplicationBuilder app, IConfiguration config)
    {
        var hangfireOptions = config.GetSection(nameof(HangfireOptions)).Get<HangfireOptions>() ?? new HangfireOptions();
        
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
