using System.Collections.Generic;
using System.Threading.Tasks;
using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Lessons.Dtos;
using Avancira.Application.Listings.Dtos;
using Microsoft.AspNetCore.Http;

public interface IListingService
{
    // Create
    Task<ListingDto> CreateListingAsync(ListingRequestDto model, string userId);

    // Read
    ListingDto GetListingById(Guid id);
    Task<PagedResult<ListingDto>> GetTutorListingsAsync(string userId, int page, int pageSize);
    IEnumerable<ListingDto> GetLandingPageListings();
    IEnumerable<ListingDto> GetLandingPageTrendingListings();
    PagedResult<ListingDto> SearchListings(string query, List<string> categories, int page, int pageSize, double? lat = null, double? lng = null, double radiusKm = 10);
    ListingStatisticsDto GetListingStatistics();

    // Update
    Task<ListingDto> UpdateListingAsync(ListingRequestDto model, string userId);
    Task<bool> ModifyListingTitleAsync(Guid listingId, string userId, string newTitle);
    Task<bool> ModifyListingImageAsync(Guid listingId, string userId, IFormFile newImage);
    Task<bool> ModifyListingLocationsAsync(Guid listingId, string userId, List<string> newLocations);
    Task<bool> ModifyListingDescriptionAsync(Guid listingId, string userId, string newAboutLesson, string newAboutYou);
    Task<bool> ModifyListingCategoryAsync(Guid listingId, string userId, Guid newCategoryId);
    Task<bool> ModifyListingRatesAsync(Guid listingId, string userId, RatesDto newRates);
    Task<bool> ToggleListingVisibilityAsync(Guid id, string userId);

    // Delete
    Task<bool> DeleteListingAsync(Guid listingId, string userId);
}
