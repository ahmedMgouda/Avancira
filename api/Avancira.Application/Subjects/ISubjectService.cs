using Avancira.Application.Subjects.Dtos;

namespace Avancira.Application.Subjects;

public interface ISubjectService
{
    Task<SubjectDto> GetByIdAsync(int id);
    Task<IEnumerable<SubjectDto>> GetAllAsync(int? categoryId = null);
    Task<SubjectDto> CreateAsync(SubjectCreateDto request);
    Task<SubjectDto> UpdateAsync(SubjectUpdateDto request);
    Task DeleteAsync(int id);
}
