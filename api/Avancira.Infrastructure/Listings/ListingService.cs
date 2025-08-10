using Avancira.Application.Catalog.Dtos;
using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Catalog;
using Avancira.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Avancira.Domain.Common.Exceptions;
using Mapster;
using Avancira.Application.Listings.Dtos;
using Avancira.Application.Lessons.Dtos;

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

        public async Task<PagedResult<ListingDto>> GetTutorListingsAsync(string userId, int page, int pageSize, CancellationToken ct = default)
        {
            var baseQuery = _dbContext.Listings
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .Include(l => l.ListingCategories).ThenInclude(lc => lc.Category);

            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(l => l.Created)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var dtos = items.Select(listing => MapToListingDto(listing)).ToList();
            return new PagedResult<ListingDto>(dtos, total, page, pageSize);
        }

        public async Task<ListingDto> CreateListingAsync(ListingRequestDto model, string userId, CancellationToken ct = default)
        {
            var validCategoryIds = await _dbContext.Categories
                .Where(c => model.CategoryIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync(ct);

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

            await _dbContext.Listings.AddAsync(listing, ct);
            await _dbContext.SaveChangesAsync(ct);

            return MapToListingDto(listing);
        }

        public async Task<ListingDto> UpdateListingAsync(ListingRequestDto model, string userId, CancellationToken ct = default)
        {
            var listing = await _dbContext.Listings
                .Include(l => l.ListingCategories)
                .FirstOrDefaultAsync(l => l.Id == model.Id && l.UserId == userId, ct);

            if (listing is null)
                throw new KeyNotFoundException("Listing not found or unauthorized.");

            var validCategoryIds = await _dbContext.Categories
                .Where(c => model.CategoryIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync(ct);

            if (!validCategoryIds.Any())
                throw new ArgumentException("No valid categories found.");

            model.Adapt(listing);

            listing.ListingCategories.Clear();
            listing.ListingCategories = validCategoryIds
                .Select(id => new ListingCategory { ListingId = listing.Id, CategoryId = id })
                .ToList();

            await _dbContext.SaveChangesAsync(ct);

            return MapToListingDto(listing);
        }

        public async Task<ListingDto> GetListingByIdAsync(Guid id, CancellationToken ct = default)
        {
            var listing = await _dbContext.Listings
                .AsNoTracking()
                .Include(l => l.ListingCategories).ThenInclude(l => l.Category)
                .FirstOrDefaultAsync(l => l.Id == id, ct);

            return listing is null ? new ListingDto() : MapToListingDto(listing);
        }

        public async Task<PagedResult<ListingDto>> GetUserListingsAsync(string userId, int page, int pageSize, CancellationToken ct = default)
        {
            var queryable = _dbContext.Listings
                .Include(l => l.ListingCategories).ThenInclude(l => l.Category)
                .Where(l => l.IsActive && l.UserId == userId);

            var totalResults = await queryable.CountAsync(ct);

            var lessons = await queryable
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var results = lessons.Select(listing => MapToListingDto(listing)).ToList();

            return new PagedResult<ListingDto>(results, totalResults, page, pageSize);
        }

        public IEnumerable<ListingDto> GetLandingPageListings()
        {
            var listings = _dbContext.Listings
                .Include(l => l.ListingCategories).ThenInclude(l => l.Category)
                .Where(l => l.IsActive && l.IsVisible)
                .OrderBy(_ => Guid.NewGuid())
                .Take(50)
                .ToList();

            return listings.Select(listing => MapToListingDto(listing));
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

        public async Task<PagedResult<ListingDto>> SearchListingsAsync(
        string? query,
        List<string>? categories,
        int page,
        int pageSize,
        double? lat = null,
        double? lng = null,
        double radiusKm = 10,
        CancellationToken ct = default)
        {
            var q = _dbContext.Listings
                .AsNoTracking()
                .Include(l => l.ListingCategories).ThenInclude(lc => lc.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var lowered = query.ToLower();
                q = q.Where(l =>
                    l.Name.ToLower().Contains(lowered) ||
                    l.Description.ToLower().Contains(lowered));
            }

            if (categories != null && categories.Count > 0)
            {
                q = q.Where(l => l.ListingCategories.Any(j => categories.Contains(j.Category.Name)));
            }

            if (lat.HasValue && lng.HasValue)
            {
                q = q.Where(l =>
                    (6371 * Math.Acos(
                        Math.Cos(Math.PI * lat.Value / 180) * Math.Cos(Math.PI * 0 / 180) *
                        Math.Cos(Math.PI * (lng.Value - 0) / 180) +
                        Math.Sin(Math.PI * lat.Value / 180) * Math.Sin(Math.PI * 0 / 180)
                    )) <= radiusKm);
            }

            q = q.OrderByDescending(l => l.Created).ThenBy(l => l.Id);

            var total = await q.CountAsync(ct);

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectToType<ListingDto>()
                .ToListAsync(ct);

            return new PagedResult<ListingDto>(items, total, page, pageSize);
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

        public async Task<bool> ModifyListingTitleAsync(Guid listingId, string userId, string newTitle, CancellationToken ct = default)
        {
            var listing = await _dbContext.Listings
                .Where(l => l.Id == listingId && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync(ct);

            if (listing == null) return false;

            listing.UpdateTitle(newTitle);

            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ModifyListingImageAsync(Guid listingId, string userId, IFormFile newImage, CancellationToken ct = default)
        {
            var listing = await _dbContext.Listings
                .Where(l => l.Id == listingId && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync(ct);

            if (listing == null) return false;

            // TODO: use _fileUploadService with ct if it supports it, then update listing image fields.
            _dbContext.Listings.Update(listing);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ModifyListingLocationsAsync(Guid listingId, string userId, List<string> newLocations, CancellationToken ct = default)
        {
            var listing = await _dbContext.Listings
                .Where(l => l.Id == listingId && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync(ct);

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

            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ModifyListingDescriptionAsync(Guid listingId, string userId, string newAboutLesson, string newAboutYou, CancellationToken ct = default)
        {
            var listing = await _dbContext.Listings
                .Where(l => l.Id == listingId && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync(ct);

            if (listing == null) return false;

            listing.UpdateDescription(newAboutLesson);
            // If you later persist AboutYou, do it here.

            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ModifyListingCategoryAsync(Guid listingId, string userId, Guid newCategoryId, CancellationToken ct = default)
        {
            var listing = await _dbContext.Listings
                .Include(l => l.ListingCategories)
                .Where(l => l.Id == listingId && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync(ct);

            if (listing == null) return false;

            var categoryExists = await _dbContext.Categories.AnyAsync(c => c.Id == newCategoryId, ct);
            if (!categoryExists) return false;

            listing.ListingCategories.Clear();
            listing.ListingCategories.Add(new ListingCategory
            {
                ListingId = listingId,
                CategoryId = newCategoryId
            });

            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ModifyListingRatesAsync(Guid listingId, string userId, RatesDto newRates, CancellationToken ct = default)
        {
            var listing = await _dbContext.Listings
                .Where(l => l.Id == listingId && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync(ct);

            if (listing == null) return false;

            listing.UpdateHourlyRate(newRates.Hourly);

            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ToggleListingVisibilityAsync(Guid id, string userId, CancellationToken ct = default)
        {
            var listing = await _dbContext.Listings
                .Where(l => l.Id == id && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync(ct);

            if (listing == null) return false;

            listing.ToggleVisibility();

            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteListingAsync(Guid listingId, string userId, CancellationToken ct = default)
        {
            var listing = await _dbContext.Listings
                .Where(l => l.Id == listingId && l.UserId == userId)
                .AsTracking()
                .FirstOrDefaultAsync(ct);

            if (listing == null || !listing.IsActive)
            {
                return false;
            }

            listing.Delete();

            await _dbContext.SaveChangesAsync(ct);
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
                Title = listing.Name,
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
