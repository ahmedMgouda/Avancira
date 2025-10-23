using Avancira.Application.StudentProfiles.Dtos;

namespace Avancira.Application.StudentProfiles;

public interface IStudentProfileService
{
    Task<StudentProfileDto> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<StudentProfileDto> UpdateAsync(StudentProfileUpdateDto request, CancellationToken cancellationToken = default);
}
