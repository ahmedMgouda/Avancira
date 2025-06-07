using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Avancira.Domain.Catalog;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class LessonCategoryService : ILessonCategoryService
    {
        private readonly AvanciraDbContext _dbContext;
        private readonly ILogger<LessonCategoryService> _logger;

        public LessonCategoryService(
            AvanciraDbContext db,
            ILogger<LessonCategoryService> logger
        )
        {
            _dbContext = db;
            _logger = logger;
        }

        public LessonCategoryDto CreateCategory(Category category)
        {
            try
            {
                _dbContext.Categories.Add(category);
                _dbContext.SaveChanges();
                
                _logger.LogInformation("Category created successfully with ID: {CategoryId}", category.Id);
                
                return MapToLessonCategoryDto(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category: {CategoryName}", category.Name);
                throw;
            }
        }

        public bool DeleteCategory(int id)
        {
            try
            {
                var category = _dbContext.Categories.Find(id);
                if (category == null)
                {
                    _logger.LogWarning("Category with ID {CategoryId} not found for deletion", id);
                    return false;
                }

                _dbContext.Categories.Remove(category);
                _dbContext.SaveChanges();
                
                _logger.LogInformation("Category deleted successfully with ID: {CategoryId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category with ID: {CategoryId}", id);
                throw;
            }
        }

        public List<LessonCategoryDto> GetLandingPageCategories()
        {
            try
            {
                var categories = _dbContext.Categories
                    .Where(category => category.DisplayInLandingPage)
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(12)
                    .ToList();

                var categoryDtos = new List<LessonCategoryDto>();
                foreach (var category in categories)
                {
                    var courseCount = _dbContext.ListingCategories
                        .Include(lc => lc.Listing)
                        .Count(lc => lc.CategoryId == category.Id && 
                                    lc.Listing.IsActive && 
                                    lc.Listing.IsVisible);
                    
                    categoryDtos.Add(MapToLessonCategoryDto(category, courseCount));
                }

                _logger.LogInformation("Retrieved {Count} landing page categories", categoryDtos.Count);
                return categoryDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving landing page categories");
                throw;
            }
        }

        public async Task<PagedResult<LessonCategoryDto>> SearchCategoriesAsync(string? query, int page, int pageSize)
        {
            try
            {
                var categoriesQuery = _dbContext.Categories.AsQueryable();

                // Apply search filter if query is provided
                if (!string.IsNullOrWhiteSpace(query))
                {
                    categoriesQuery = categoriesQuery.Where(c => c.Name.Contains(query));
                }

                // Get total count for pagination
                var totalCount = await categoriesQuery.CountAsync();

                // Apply pagination and get results
                var categories = await categoriesQuery
                    .OrderBy(c => c.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to DTOs with course counts
                var categoryDtos = new List<LessonCategoryDto>();
                foreach (var category in categories)
                {
                    var courseCount = await _dbContext.ListingCategories
                        .Include(lc => lc.Listing)
                        .CountAsync(lc => lc.CategoryId == category.Id && 
                                         lc.Listing.IsActive && 
                                         lc.Listing.IsVisible);
                    
                    categoryDtos.Add(MapToLessonCategoryDto(category, courseCount));
                }

                _logger.LogInformation("Search completed for query: {Query}, found {Count} categories", query, totalCount);

                return new PagedResult<LessonCategoryDto>(categoryDtos, totalCount, page, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching categories with query: {Query}", query);
                throw;
            }
        }

        public LessonCategoryDto UpdateCategory(int id, Category updatedCategory)
        {
            try
            {
                var existingCategory = _dbContext.Categories.Find(id);
                if (existingCategory == null)
                {
                    _logger.LogWarning("Category with ID {CategoryId} not found for update", id);
                    throw new InvalidOperationException($"Category with ID {id} not found");
                }

                // Update properties
                existingCategory.Name = updatedCategory.Name;
                existingCategory.DisplayInLandingPage = updatedCategory.DisplayInLandingPage;
                existingCategory.ImageUrl = updatedCategory.ImageUrl;

                _dbContext.SaveChanges();
                
                _logger.LogInformation("Category updated successfully with ID: {CategoryId}", id);
                
                return MapToLessonCategoryDto(existingCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category with ID: {CategoryId}", id);
                throw;
            }
        }

        private static LessonCategoryDto MapToLessonCategoryDto(Category category, int? courses = null)
        {
            return new LessonCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Image = category.ImageUrl?.ToString(),
                Courses = courses ?? 0
            };
        }
    }
}
