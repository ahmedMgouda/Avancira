using Avancira.Application.Catalog.Categories.Dtos;
using Avancira.Application.Services.Category;
using Avancira.Domain.Catalog;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class CategoryService : ICategoryService
    {
        private readonly AvanciraDbContext _dbContext;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(
            AvanciraDbContext dbContext,
            ILogger<CategoryService> logger
        )
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<CategoryDto> GetByIdAsync(Guid id)
        {
            try
            {
                var category = await _dbContext.Categories.FindAsync(id);
                if (category == null)
                    throw new KeyNotFoundException($"Category with ID {id} not found");

                return MapToDto(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category by ID {CategoryId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            try
            {
                var categories = await _dbContext.Categories
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return categories.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories");
                throw;
            }
        }

        public async Task<CategoryDto> CreateAsync(CategoryCreateDto createDto)
        {
            try
            {
                if (createDto == null)
                    throw new ArgumentNullException(nameof(createDto));

                Uri? imageUrl = null;
                
                // Handle image upload if provided
                if (createDto.Image != null && !string.IsNullOrEmpty(createDto.Image.Name))
                {
                    // In a real implementation, you would:
                    // 1. Save the uploaded file using IFileUploadService
                    // 2. Get the URL of the saved file
                    // For now, we'll create a placeholder URL
                    imageUrl = new Uri($"https://placeholder.com/categories/{Guid.NewGuid()}.jpg");
                }

                var category = new Category
                {
                    Name = createDto.Name,
                    DisplayInLandingPage = createDto.DisplayInLandingPage,
                    ImageUrl = imageUrl
                };

                await _dbContext.Categories.AddAsync(category);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Category created successfully with ID {CategoryId}", category.Id);
                return MapToDto(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                throw;
            }
        }

        public async Task<CategoryDto> UpdateAsync(CategoryUpdateDto updateDto)
        {
            try
            {
                if (updateDto == null)
                    throw new ArgumentNullException(nameof(updateDto));

                var category = await _dbContext.Categories.FindAsync(updateDto.Id);
                if (category == null)
                    throw new KeyNotFoundException($"Category with ID {updateDto.Id} not found");

                category.Name = updateDto.Name;
                category.DisplayInLandingPage = updateDto.DisplayInLandingPage;

                // Handle image updates
                if (updateDto.DeleteCurrentImage)
                {
                    // Delete current image
                    category.ImageUrl = null;
                }
                else if (updateDto.Image != null && !string.IsNullOrEmpty(updateDto.Image.Name))
                {
                    // Upload new image
                    // In a real implementation, you would:
                    // 1. Delete the old image file if it exists
                    // 2. Save the new uploaded file using IFileUploadService
                    // 3. Get the URL of the saved file
                    // For now, we'll create a placeholder URL
                    category.ImageUrl = new Uri($"https://placeholder.com/categories/{Guid.NewGuid()}.jpg");
                }

                _dbContext.Categories.Update(category);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Category updated successfully with ID {CategoryId}", category.Id);
                return MapToDto(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category with ID {CategoryId}", updateDto?.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var category = await _dbContext.Categories.FindAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Category with ID {CategoryId} not found for deletion", id);
                    return false;
                }

                _dbContext.Categories.Remove(category);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Category deleted successfully with ID {CategoryId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category with ID {CategoryId}", id);
                throw;
            }
        }

        private static CategoryDto MapToDto(Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                DisplayInLandingPage = category.DisplayInLandingPage,
                ImageUrl = category.ImageUrl
            };
        }
    }
}
