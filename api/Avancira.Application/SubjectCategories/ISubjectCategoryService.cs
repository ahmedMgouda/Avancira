using Avancira.Application.SubjectCategories.Dtos;

namespace Avancira.Application.SubjectCategories;

public interface ISubjectCategoryService
{
    Task<SubjectCategoryDto> GetByIdAsync(int id);
    Task<IEnumerable<SubjectCategoryDto>> GetAllAsync();
    Task<SubjectCategoryDto> CreateAsync(SubjectCategoryCreateDto request);
    Task<SubjectCategoryDto> UpdateAsync(SubjectCategoryUpdateDto request);
    Task DeleteAsync(int id);
}
