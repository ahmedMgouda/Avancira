using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Catalog;
using Avancira.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Avancira.Domain.Common.Exceptions;
using Mapster;
using Avancira.Application.Catalog.Listings.Dtos;

namespace Avancira.Infrastructure.Catalog
{
    public class ListingService : IListingService
    {
        private readonly AvanciraDbContext _dbContext;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<ListingService> _logger;

        public ListingService(
            AvanciraDbContext dbContext,
            IFileUploadService fileUploadService,
            ILogger<ListingService> logger
        )
        {
            _dbContext = dbContext;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        public async Task<PagedResult<ListingDto>> GetTutorListingsAsync(string userId, int page, int pageSize)
        {
            var query = _dbContext.Listings
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .Include(l => l.ListingCategories)
                    .ThenInclude(llc => llc.Category);

            var totalRecords = await query.CountAsync();

            var listings = await query
                .OrderByDescending(l => l.Created)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var listingDtos = listings.Select(listing => MapToListingDto(listing)).ToList();

            return new PagedResult<ListingDto>(listingDtos, totalRecords, page, pageSize);
        }

        public async Task<ListingDto> CreateListingAsync(ListingRequestDto model, string userId)
        {
            // Validate that the provided category IDs exist in the Categories table
            var validCategoryIds = await _dbContext.Categories
                .Where(c => model.CategoryIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            if (!validCategoryIds.Any())
                throw new BadRequestException("No valid categories were found.");

            var listing = model.Adapt<Listing>();

            listing.UserId = userId;

            listing.ListingCategories = validCategoryIds
                .Select(categoryId => new ListingCategory
                {
                    ListingId = listing.Id,
                    CategoryId = categoryId
                })
                .ToList();

            await _dbContext.Listings.AddAsync(listing);
            await _dbContext.SaveChangesAsync();

            return MapToListingDto(listing);

        }

        public async Task<ListingDto> UpdateListingAsync(ListingRequestDto model, string userId)
        {
            var listing = await _dbContext.Listings
                .Include(l => l.ListingCategories)
                .FirstOrDefaultAsync(l => l.Id == model.Id && l.UserId == userId);

            if (listing is null)
                throw new KeyNotFoundException("Listing not found or unauthorized.");

            var validCategoryIds = await _dbContext.Categories
                .Where(c => model.CategoryIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            if (!validCategoryIds.Any())
                throw new ArgumentException("No valid categories found.");

            listing.ListingCategories.Clear();
            listing.ListingCategories = validCategoryIds
                .Select(id => new ListingCategory { ListingId = listing.Id, CategoryId = id })
                .ToList();

            listing = model.Adapt<Listing>();

            await _dbContext.SaveChangesAsync();

            return MapToListingDto(listing);
        }

        public ListingDto GetListingById(Guid id)
        {
            var listing = _dbContext.Listings
                .Include(l => l.ListingCategories).ThenInclude(l => l.Category)
                .FirstOrDefault(l => l.Id == id);

            return listing == null ? new ListingDto() : MapToListingDto(listing);
        }

        public async Task<PagedResult<ListingDto>> GetUserListingsAsync(string userId, int page, int pageSize)
        {
            var queryable = _dbContext.Listings
                .Include(l => l.ListingCategories).ThenInclude(l => l.Category)
                .Where(l => l.IsActive && l.UserId == userId);

            // Get total count before pagination
            var totalResults = await queryable.CountAsync();

            // Apply pagination
            var lessons = await queryable
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var results = lessons.Select(listing => MapToListingDto(listing)).ToList();

            return new PagedResult<ListingDto>
            (
               results: results,
               totalResults: totalResults,
               page: page,
               pageSize: pageSize
            );
        }

        public IEnumerable<ListingDto> GetLandingPageListings()
        {
            var listings = _dbContext.Listings
                .Include(l => l.ListingCategories).ThenInclude(l => l.Category)
                .Where(l => l.IsActive && l.IsVisible)
                .OrderBy(_ => Guid.NewGuid())
                .Take(50)
                .ToList();

            return listings.Select(l => MapToListingDto(l));
        }
        public IEnumerable<ListingDto> GetLandingPageTrendingListings()
        {
            var listings = _dbContext.Listings
                .Include(l => l.ListingCategories).ThenInclude(l => l.Category)
                .Where(l => l.IsActive && l.IsVisible)
                .OrderBy(_ => Guid.NewGuid())
                .Take(10)
                .Select(l => new { Listing = l, l.UserId })
                .ToList();

            var userIds = listings.Select(x => x.UserId).Distinct().ToList();

            var users = _dbContext.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.ImageUrl);

            var dtos = listings.Select(x =>
            {
                var dto = MapToListingDto(x.Listing);
                dto.ListingImagePath = users.TryGetValue(x.UserId, out var img) ? img?.ToString() : null;
                return dto;
            });

            return dtos;
        }

        public PagedResult<ListingDto> SearchListings(string query, List<string> categories, int page, int pageSize, double? lat = null, double? lng = null, double radiusKm = 10)
        {
            var queryable = _dbContext.Listings
                .Include(l => l.ListingCategories).ThenInclude(l => l.Category)
                .Where(l => EF.Functions.Like(l.Name, $"%{query}%") ||
                            EF.Functions.Like(l.Description, $"%{query}%"));
            // Apply category filtering if categories are selected
            if (categories.Any())
            {
                queryable = queryable.Where(l => l.ListingCategories.Any(j => categories.Contains(j.Category.Name)));
            }

            if (lat.HasValue && lng.HasValue)
            {
                // Haversine formula to calculate distance in KM
                queryable = queryable.Where(l =>
                    (6371 * Math.Acos(
                        Math.Cos(Math.PI * lat.Value / 180) * Math.Cos(Math.PI * 0 / 180) *
                        Math.Cos(Math.PI * (lng.Value - 0) / 180) +
                        Math.Sin(Math.PI * lat.Value / 180) * Math.Sin(Math.PI * 0 / 180)
                    )) <= radiusKm);
            }

            var totalResults = queryable.Count();
            var listings = queryable
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var results = listings.Select(l => MapToListingDto(l));

            return new PagedResult<ListingDto>(
                results: results,
                totalResults: totalResults,
                page: page,
                pageSize: pageSize
            );
        }

        public ListingStatisticsDto GetListingStatistics()
        {
            var today = DateTime.UtcNow.Date;
            var startOfDay = today;
            var endOfDay = today.AddDays(1);

            var stats = _dbContext.Listings
                .AsNoTracking()
                .Where(l => l.IsActive && l.IsVisible)
                .GroupBy(_ => 1)
                .Select(g => new ListingStatisticsDto
                {
                    TotalListings = g.Count(),
                    NewListingsToday = g.Count(l => startOfDay <= l.Created && l.Created < endOfDay)
                })
                .FirstOrDefault() ?? new ListingStatisticsDto { TotalListings = 0, NewListingsToday = 0 };

            return stats;
        }

        public async Task<bool> ModifyListingTitleAsync(Guid listingId, string userId, string newTitle)
        {
            var listing = await _dbContext.Listings
                .Where(l => l.Id == listingId && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync();

            if (listing == null) return false;

            listing.UpdateTitle(newTitle);

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ModifyListingImageAsync(Guid listingId, string userId, IFormFile newImage)
        {
            var listing = await _dbContext.Listings
                .Where(l => l.Id == listingId && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync();

            if (listing == null) return false;

            _dbContext.Listings.Update(listing);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ModifyListingLocationsAsync(Guid listingId, string userId, List<string> newLocations)
        {
            var listing = await _dbContext.Listings
                .Where(l => l.Id == listingId && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync();

            if (listing == null) return false;

            var updatedLocations = newLocations
                .Select(location =>
                {
                    if (Enum.TryParse<ListingLocationType>(location, true, out var parsedLocation))
                    {
                        return parsedLocation;
                    }
                    return ListingLocationType.None;
                })
                .Aggregate(ListingLocationType.None, (current, parsedLocation) => current | parsedLocation);

            listing.UpdateLocations(updatedLocations);

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ModifyListingDescriptionAsync(Guid listingId, string userId, string newAboutLesson, string newAboutYou)
        {
            var listing = await _dbContext.Listings
                .Where(l => l.Id == listingId && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync();

            if (listing == null) return false;

            listing.UpdateDescription(newAboutLesson);

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ModifyListingCategoryAsync(Guid listingId, string userId, Guid newCategoryId)
        {
            var listing = await _dbContext.Listings
                .Include(l => l.ListingCategories)
                .Where(l => l.Id == listingId && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync();

            if (listing == null) return false;

            var categoryExists = await _dbContext.Categories.AnyAsync(c => c.Id == newCategoryId);
            if (!categoryExists) return false;

            listing.ListingCategories.Clear();
            listing.ListingCategories.Add(new ListingCategory 
            { 
                ListingId = listingId,
                CategoryId = newCategoryId 
            });
            
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ModifyListingRatesAsync(Guid listingId, string userId, RatesDto newRates)
        {
            var listing = await _dbContext.Listings
                .Where(l => l.Id == listingId && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync();

            if (listing == null) return false;

            listing.UpdateHourlyRate(newRates.Hourly);

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleListingVisibilityAsync(Guid id)
        {
            var listing = await _dbContext.Listings
                .Where(l => l.Id == id)
                .AsTracking()
                .FirstOrDefaultAsync();

            if (listing == null) return false;

            listing.ToggleVisibility();

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteListingAsync(Guid listingId, string userId)
        {
            var listing = await _dbContext.Listings
                .Where(l => l.Id == listingId && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync();

            if (listing == null || !listing.IsActive)
            {
                return false;
            }

            listing.Delete();

            await _dbContext.SaveChangesAsync();
            return true;
        }

        private ListingDto MapToListingDto(Listing listing, int? contactedCount = null)
        {
            return new ListingDto
            {
                Id = listing.Id,
                IsVisible = listing.IsVisible,
                ContactedCount = contactedCount ?? 0,
                Reviews = 0,
                //Category = listing.ListingCategories.FirstOrDefault()?.Category?.Name ?? "Unknown",
                Title = listing.Name,
                //ListingImagePath = listing.UserId,
                //Locations = Enum.GetValues(typeof(ListingLocationType))
                //                .Cast<ListingLocationType>()
                //                .Where(location => (listing.Locations & location) == location && location != ListingLocationType.None)
                //                .Select(location => location.ToString())
                //                .ToList(),
                AboutLesson = listing.Description,
                AboutYou = string.Empty,
                Rate = $"{listing.HourlyRate}/h",
                Rates = new RatesDto
                {
                    Hourly = listing.HourlyRate,
                    FiveHours = listing.HourlyRate * 5,
                    TenHours = listing.HourlyRate * 10
                },
                SocialPlatforms = new List<string> { "Messenger", "Linkedin", "Facebook", "Email" }
            };
        }
    }
}
