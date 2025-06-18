using System.Linq.Expressions;
using Avancira.Application.Jobs;

namespace Avancira.Infrastructure.Jobs;

public class StubJobService : IJobService
{
    public bool Delete(string jobId)
    {
        // Stub implementation - always return true
        return true;
    }

    public bool Delete(string jobId, string fromState)
    {
        // Stub implementation - always return true
        return true;
    }

    public string Enqueue(Expression<Action> methodCall)
    {
        // Stub implementation - return a fake job ID
        return Guid.NewGuid().ToString();
    }

    public string Enqueue(string queue, Expression<Func<Task>> methodCall)
    {
        // Stub implementation - return a fake job ID
        return Guid.NewGuid().ToString();
    }

    public string Enqueue(Expression<Func<Task>> methodCall)
    {
        // Stub implementation - return a fake job ID
        return Guid.NewGuid().ToString();
    }

    public string Enqueue<T>(Expression<Action<T>> methodCall)
    {
        // Stub implementation - return a fake job ID
        return Guid.NewGuid().ToString();
    }

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
    {
        // Stub implementation - return a fake job ID
        return Guid.NewGuid().ToString();
    }

    public bool Requeue(string jobId)
    {
        // Stub implementation - always return true
        return true;
    }

    public bool Requeue(string jobId, string fromState)
    {
        // Stub implementation - always return true
        return true;
    }

    public string Schedule(Expression<Action> methodCall, TimeSpan delay)
    {
        // Stub implementation - return a fake job ID
        return Guid.NewGuid().ToString();
    }

    public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay)
    {
        // Stub implementation - return a fake job ID
        return Guid.NewGuid().ToString();
    }

    public string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt)
    {
        // Stub implementation - return a fake job ID
        return Guid.NewGuid().ToString();
    }

    public string Schedule(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt)
    {
        // Stub implementation - return a fake job ID
        return Guid.NewGuid().ToString();
    }

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay)
    {
        // Stub implementation - return a fake job ID
        return Guid.NewGuid().ToString();
    }

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
    {
        // Stub implementation - return a fake job ID
        return Guid.NewGuid().ToString();
    }

    public string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt)
    {
        // Stub implementation - return a fake job ID
        return Guid.NewGuid().ToString();
    }

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt)
    {
        // Stub implementation - return a fake job ID
        return Guid.NewGuid().ToString();
    }
}
