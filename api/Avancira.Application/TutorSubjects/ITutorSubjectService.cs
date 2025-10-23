using Avancira.Application.TutorSubjects.Dtos;

namespace Avancira.Application.TutorSubjects;

public interface ITutorSubjectService
{
    Task<IReadOnlyCollection<TutorSubjectDto>> GetByTutorIdAsync(string tutorId, CancellationToken cancellationToken = default);
    Task<TutorSubjectDto> CreateAsync(TutorSubjectCreateDto request, CancellationToken cancellationToken = default);
    Task<TutorSubjectDto> UpdateAsync(TutorSubjectUpdateDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
