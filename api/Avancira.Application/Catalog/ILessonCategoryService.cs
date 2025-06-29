using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Lessons.Dtos;
using Avancira.Domain.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ILessonCategoryService
{
    // Create
    LessonCategoryDto CreateCategory(Category category);

    // Read
    List<LessonCategoryDto> GetLandingPageCategories();
    Task<PagedResult<LessonCategoryDto>> SearchCategoriesAsync(string? query, int page, int pageSize);

    // Update
    LessonCategoryDto UpdateCategory(int id, Category updatedCategory);

    // Delete
    bool DeleteCategory(int id);
}
