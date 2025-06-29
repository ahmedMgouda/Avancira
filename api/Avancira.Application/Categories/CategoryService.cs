using Avancira.Domain.Catalog;
using Avancira.Domain.Common.Exceptions;
using Avancira.Application.Persistence;
using Mapster;
using Avancira.Application.Storage.File;
using Avancira.Application.Storage;
using Avancira.Application.Categories.Dtos;

namespace Avancira.Application.Categories
{
    public class CategoryService : ICategoryService
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly IStorageService _storageService;

        public CategoryService(IRepository<Category> categoryRepository, IStorageService storageService)
        {
            _categoryRepository = categoryRepository;
            _storageService = storageService;
        }

        public async Task<CategoryDto> GetByIdAsync(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Category with ID '{id}' not found.");

            return category.Adapt<CategoryDto>();
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var categories = await _categoryRepository.ListAsync();
            return categories.Adapt<IEnumerable<CategoryDto>>();
        }

        public async Task<CategoryDto> CreateAsync(CategoryCreateDto categoryDto)
        {
            if (categoryDto == null) throw new ArgumentNullException(nameof(categoryDto));

            var category = categoryDto.Adapt<Category>();

            if (categoryDto.Image != null)
            {
                var imageUri = await _storageService.UploadAsync<Category>(categoryDto.Image, FileType.Image);
                category.ImageUrl = imageUri;
            }

            await _categoryRepository.AddAsync(category);
            return category.Adapt<CategoryDto>();
        }

        public async Task<CategoryDto> UpdateAsync(CategoryUpdateDto categoryDto)
        {
            if (categoryDto == null) throw new ArgumentNullException(nameof(categoryDto));

            var category = await _categoryRepository.GetByIdAsync(categoryDto.Id)
                ?? throw new NotFoundException($"Category with ID '{categoryDto.Id}' not found. Update operation aborted.");

            category.Name = categoryDto.Name;
            category.DisplayInLandingPage = categoryDto.DisplayInLandingPage;

            var currentImageUrl = category.ImageUrl;

            if (categoryDto.DeleteCurrentImage && currentImageUrl != null)
            {
                _storageService.Remove(currentImageUrl);
                category.ImageUrl = null;
            }

            if (categoryDto.Image != null)
            {
                var newImageUrl = await _storageService.UploadAsync<Category>(categoryDto.Image, FileType.Image);

                // Remove old image only if not already removed
                if (!categoryDto.DeleteCurrentImage && currentImageUrl != null)
                    _storageService.Remove(currentImageUrl);

                category.ImageUrl = newImageUrl;
            }

            await _categoryRepository.UpdateAsync(category);
            return category.Adapt<CategoryDto>();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Category with ID '{id}' not found. Deletion operation aborted.");

            if (category.ImageUrl != null)
            {
                _storageService.Remove(category.ImageUrl);
            }

            await _categoryRepository.DeleteAsync(category);
            return true;
        }
    }
}
