using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Lessons.Dtos;
using Avancira.Application.Listings.Dtos;
using Microsoft.AspNetCore.Http;

public interface IListingService
{
    // Create
    Task<ListingDto> CreateListingAsync(ListingRequestDto model, string userId, CancellationToken cancellationToken = default);

    // Read
    Task<ListingDto> GetListingByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<ListingDto>> GetTutorListingsAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default);
    IEnumerable<ListingDto> GetLandingPageListings();
    IEnumerable<ListingDto> GetLandingPageTrendingListings();
    Task<PagedResult<ListingDto>> SearchListingsAsync(string query, List<string> categories, int page, int pageSize, double? lat = null, double? lng = null, double radiusKm = 10, CancellationToken ct = default);
    ListingStatisticsDto GetListingStatistics();

    // Update
    Task<ListingDto> UpdateListingAsync(ListingRequestDto model, string userId, CancellationToken cancellationToken = default);
    Task<bool> ModifyListingTitleAsync(Guid listingId, string userId, string newTitle, CancellationToken cancellationToken = default);
    Task<bool> ModifyListingImageAsync(Guid listingId, string userId, IFormFile newImage, CancellationToken cancellationToken = default);
    Task<bool> ModifyListingLocationsAsync(Guid listingId, string userId, List<string> newLocations, CancellationToken cancellationToken = default);
    Task<bool> ModifyListingDescriptionAsync(Guid listingId, string userId, string newAboutLesson, string newAboutYou, CancellationToken cancellationToken = default);
    Task<bool> ModifyListingCategoryAsync(Guid listingId, string userId, Guid newCategoryId, CancellationToken cancellationToken = default);
    Task<bool> ModifyListingRatesAsync(Guid listingId, string userId, RatesDto newRates, CancellationToken cancellationToken = default);
    Task<bool> ToggleListingVisibilityAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    // Delete
    Task<bool> DeleteListingAsync(Guid listingId, string userId, CancellationToken cancellationToken = default);
}
