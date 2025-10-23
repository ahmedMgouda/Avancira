using Avancira.Application.TutorProfiles.Dtos;

namespace Avancira.Application.TutorProfiles;

public interface ITutorProfileService
{
    Task<TutorProfileDto> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<TutorProfileDto> UpdateAsync(TutorProfileUpdateDto request, CancellationToken cancellationToken = default);
    Task<TutorProfileDto> VerifyAsync(TutorProfileVerificationDto request, CancellationToken cancellationToken = default);
}
