using System.Collections.Generic;
using System.Threading.Tasks;
using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Catalog.Listings.Dtos;
using Microsoft.AspNetCore.Http;

public interface IListingService
{
    // Create
    Task<ListingDto> CreateListingAsync(ListingRequestDto model, Guid userId);

    // Read
    ListingDto GetListingById(Guid id);
    Task<PagedResult<ListingDto>> GetTutorListingsAsync(Guid userId, int page, int pageSize);
    IEnumerable<ListingDto> GetLandingPageListings();
    IEnumerable<ListingDto> GetLandingPageTrendingListings();
    PagedResult<ListingDto> SearchListings(string query, List<string> categories, int page, int pageSize, double? lat = null, double? lng = null, double radiusKm = 10);
    ListingStatisticsDto GetListingStatistics();

    // Update
    Task<ListingDto> UpdateListingAsync(ListingRequestDto model, Guid userId);
    Task<bool> ModifyListingTitleAsync(Guid listingId, Guid userId, string newTitle);
    Task<bool> ModifyListingImageAsync(Guid listingId, Guid userId, IFormFile newImage);
    Task<bool> ModifyListingLocationsAsync(Guid listingId, Guid userId, List<string> newLocations);
    Task<bool> ModifyListingDescriptionAsync(Guid listingId, Guid userId, string newAboutLesson, string newAboutYou);
    Task<bool> ModifyListingCategoryAsync(Guid listingId, Guid userId, Guid newCategoryId);
    Task<bool> ModifyListingRatesAsync(Guid listingId, Guid userId, RatesDto newRates);
    Task<bool> ToggleListingVisibilityAsync(Guid id);

    // Delete
    Task<bool> DeleteListingAsync(Guid listingId, Guid userId);
}

