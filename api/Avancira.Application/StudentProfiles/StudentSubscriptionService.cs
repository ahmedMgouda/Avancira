using Avancira.Application.Persistence;
using Avancira.Application.StudentProfiles.Specifications;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Students;

namespace Avancira.Application.StudentProfiles
{
    internal sealed class StudentSubscriptionService : IStudentSubscriptionService
    {
        private readonly IRepository<StudentProfile> _repository;

        public StudentSubscriptionService(IRepository<StudentProfile> repository)
        {
            _repository = repository;
        }

        public async Task ActivateAsync(string studentId, DateTime startUtc, DateTime endUtc, CancellationToken ct)
        {
            var profile = await GetAsync(studentId, ct);
            profile.ActivateSubscription(startUtc, endUtc);
            await _repository.UpdateAsync(profile, ct);
        }

        public async Task StartTrialAsync(string studentId, DateTime startUtc, DateTime endUtc, CancellationToken ct)
        {
            var profile = await GetAsync(studentId, ct);
            profile.StartTrial(startUtc, endUtc);
            await _repository.UpdateAsync(profile, ct);
        }

        public async Task MarkPastDueAsync(string studentId, CancellationToken ct)
        {
            var profile = await GetAsync(studentId, ct);
            profile.MarkPaymentPastDue();
            await _repository.UpdateAsync(profile, ct);
        }

        public async Task SuspendAsync(string studentId, CancellationToken ct)
        {
            var profile = await GetAsync(studentId, ct);
            profile.SuspendSubscription();
            await _repository.UpdateAsync(profile, ct);
        }

        public async Task CancelAsync(string studentId, DateTime endUtc, CancellationToken ct)
        {
            var profile = await GetAsync(studentId, ct);
            profile.CancelSubscription(endUtc);
            await _repository.UpdateAsync(profile, ct);
        }

        public async Task ExpireAsync(string studentId, CancellationToken ct)
        {
            var profile = await GetAsync(studentId, ct);
            profile.ExpireSubscription();
            await _repository.UpdateAsync(profile, ct);
        }

        public async Task ResetAsync(string studentId, CancellationToken ct)
        {
            var profile = await GetAsync(studentId, ct);
            profile.ResetSubscription();
            await _repository.UpdateAsync(profile, ct);
        }

        private async Task<StudentProfile> GetAsync(string id, CancellationToken ct)
        {
            var spec = new StudentProfileByIdSpec(id);
            var profile = await _repository.FirstOrDefaultAsync(spec, ct);

            if (profile is null)
                throw new AvanciraNotFoundException($"Student profile not found for user {id}");

            return profile;
        }
    }
}
