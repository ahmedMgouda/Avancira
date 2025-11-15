using Avancira.Application.Common.Models;
using Avancira.Application.SubjectCategories.Dtos;

namespace Avancira.Application.SubjectCategories;

public interface ISubjectCategoryService
{
    Task<PaginatedResult<SubjectCategoryDto>> GetAllAsync(SubjectCategoryFilter filter);
    Task<SubjectCategoryDto> GetByIdAsync(int id);
    Task<SubjectCategoryDto> CreateAsync(SubjectCategoryCreateDto request);
    Task<SubjectCategoryDto> UpdateAsync(SubjectCategoryUpdateDto request);
    Task DeleteAsync(int id);
    Task ReorderAsync(int[] categoryIds);
    Task MoveToPositionAsync(int id, int targetSortOrder);
}
