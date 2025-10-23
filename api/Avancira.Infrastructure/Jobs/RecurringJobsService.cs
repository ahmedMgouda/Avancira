using Avancira.Application.Jobs;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Jobs;

public class RecurringJobsService : IHostedService
{
    private readonly ILogger<RecurringJobsService> _logger;

    public RecurringJobsService(ILogger<RecurringJobsService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting up recurring jobs...");

        try
        {
            // Set up your payment processing recurring jobs

            // Monthly payment processing - 1st day of each month at 00:00 UTC
            RecurringJob.AddOrUpdate<IPaymentJobService>(
                "monthly-payment-processing",
                service => service.ProcessMonthlyPaymentsAsync(),
                Cron.Monthly(1)); // 1st day of every month at midnight

            // Daily subscription renewal processing - every day at midnight UTC
            RecurringJob.AddOrUpdate<IPaymentJobService>(
                "daily-subscription-renewals",
                service => service.ProcessDailySubscriptionRenewalsAsync(),
                Cron.Daily()); // Every day at midnight

            _logger.LogInformation("Recurring jobs set up successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set up recurring jobs");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping recurring jobs service...");
        return Task.CompletedTask;
    }
}
