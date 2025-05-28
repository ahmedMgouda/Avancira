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
            throw new NotImplementedException();
        }

        public bool DeleteCategory(int id)
        {
            throw new NotImplementedException();
        }

        public List<LessonCategoryDto> GetLandingPageCategories()
        {
            return _dbContext.Categories
                .OrderBy(_ => Guid.NewGuid())
                .Where(category => category.DisplayInLandingPage)
                .Take(12)
                .Select(category => MapToLessonCategoryDto(category, _dbContext.ListingCategories.Include(l => l.Listing).Count(l => true /*l.Listing.Active && l.Listing.IsVisible && l.LessonCategoryId == category.Id*/)))
                .ToList();
        }

        public Task<PagedResult<LessonCategoryDto>> SearchCategoriesAsync(string? query, int page, int pageSize)
        {
            throw new NotImplementedException();
        }

        public LessonCategoryDto UpdateCategory(int id, Category updatedCategory)
        {
            throw new NotImplementedException();
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
