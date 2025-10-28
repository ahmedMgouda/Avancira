namespace Avancira.Application.StudentProfiles
{
    public interface IStudentSubscriptionService
    {
        Task ActivateAsync(string studentId, DateTime startUtc, DateTime endUtc, CancellationToken ct);
        Task StartTrialAsync(string studentId, DateTime startUtc, DateTime endUtc, CancellationToken ct);
        Task MarkPastDueAsync(string studentId, CancellationToken ct);
        Task SuspendAsync(string studentId, CancellationToken ct);
        Task CancelAsync(string studentId, DateTime endUtc, CancellationToken ct);
        Task ExpireAsync(string studentId, CancellationToken ct);
        Task ResetAsync(string studentId, CancellationToken ct);
    }
}
