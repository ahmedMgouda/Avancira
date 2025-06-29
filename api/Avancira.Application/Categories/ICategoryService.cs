using Avancira.Application.Categories.Dtos;

namespace Avancira.Application.Categories
{
    public interface ICategoryService
    {
        Task<CategoryDto> GetByIdAsync(Guid id);
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<CategoryDto> CreateAsync(CategoryCreateDto createDto);
        Task<CategoryDto> UpdateAsync(CategoryUpdateDto updateDto);
        Task<bool> DeleteAsync(Guid id);
    }
}
