using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Avancira.Application.Jobs;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Jobs;

internal sealed class DisabledJobService : IJobService
{
    private readonly ILogger<DisabledJobService> _logger;

    public DisabledJobService(ILogger<DisabledJobService> logger)
    {
        _logger = logger;
    }

    private InvalidOperationException CreateException() =>
        new InvalidOperationException("Hangfire is disabled for this application instance.");

    private void LogDisabledInvocation(string operation)
    {
        _logger.LogWarning("Attempted to invoke '{Operation}' but Hangfire is disabled for this host.", operation);
    }

    public bool Delete(string jobId)
    {
        LogDisabledInvocation(nameof(Delete));
        throw CreateException();
    }

    public bool Delete(string jobId, string fromState)
    {
        LogDisabledInvocation(nameof(Delete));
        throw CreateException();
    }

    public string Enqueue(Expression<Action> methodCall)
    {
        LogDisabledInvocation(nameof(Enqueue));
        throw CreateException();
    }

    public string Enqueue(string queue, Expression<Func<Task>> methodCall)
    {
        LogDisabledInvocation(nameof(Enqueue));
        throw CreateException();
    }

    public string Enqueue(Expression<Func<Task>> methodCall)
    {
        LogDisabledInvocation(nameof(Enqueue));
        throw CreateException();
    }

    public string Enqueue<T>(Expression<Action<T>> methodCall)
    {
        LogDisabledInvocation(nameof(Enqueue));
        throw CreateException();
    }

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
    {
        LogDisabledInvocation(nameof(Enqueue));
        throw CreateException();
    }

    public bool Requeue(string jobId)
    {
        LogDisabledInvocation(nameof(Requeue));
        throw CreateException();
    }

    public bool Requeue(string jobId, string fromState)
    {
        LogDisabledInvocation(nameof(Requeue));
        throw CreateException();
    }

    public string Schedule(Expression<Action> methodCall, TimeSpan delay)
    {
        LogDisabledInvocation(nameof(Schedule));
        throw CreateException();
    }

    public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay)
    {
        LogDisabledInvocation(nameof(Schedule));
        throw CreateException();
    }

    public string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt)
    {
        LogDisabledInvocation(nameof(Schedule));
        throw CreateException();
    }

    public string Schedule(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt)
    {
        LogDisabledInvocation(nameof(Schedule));
        throw CreateException();
    }

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay)
    {
        LogDisabledInvocation(nameof(Schedule));
        throw CreateException();
    }

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
    {
        LogDisabledInvocation(nameof(Schedule));
        throw CreateException();
    }

    public string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt)
    {
        LogDisabledInvocation(nameof(Schedule));
        throw CreateException();
    }

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt)
    {
        LogDisabledInvocation(nameof(Schedule));
        throw CreateException();
    }
}

internal sealed class DisabledPaymentJobService : IPaymentJobService
{
    private readonly ILogger<DisabledPaymentJobService> _logger;

    public DisabledPaymentJobService(ILogger<DisabledPaymentJobService> logger)
    {
        _logger = logger;
    }

    private Task DisabledAsync(string operation)
    {
        _logger.LogWarning("Attempted to run '{Operation}' but Hangfire is disabled for this host.", operation);
        throw new InvalidOperationException("Hangfire is disabled for this application instance.");
    }

    public Task ProcessDailySubscriptionRenewalsAsync() => DisabledAsync(nameof(ProcessDailySubscriptionRenewalsAsync));


    public Task ProcessMonthlyPaymentsAsync() => DisabledAsync(nameof(ProcessMonthlyPaymentsAsync));
}
