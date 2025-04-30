using System.Collections.Generic;
using System.Threading.Tasks;
using Avancira.Application.Catalog.Dtos;
using Microsoft.AspNetCore.Http;

public interface IListingService
{
    // Create
    Task<ListingResponseDto> CreateListingAsync(ListingRequestDto model, string userId);

    // Read
    ListingDto GetListingById(int id);
    Task<PagedResult<ListingResponseDto>> GetTutorListingsAsync(string userId, int page, int pageSize);
    IEnumerable<ListingDto> GetLandingPageListings();
    IEnumerable<ListingDto> GetLandingPageTrendingListings();
    PagedResult<ListingDto> SearchListings(string query, List<string> categories, int page, int pageSize, double? lat = null, double? lng = null, double radiusKm = 10);
    ListingStatisticsDto GetListingStatistics();

    // Update
    Task<ListingResponseDto> UpdateListingAsync(ListingRequestDto model, string userId);
    Task<bool> ModifyListingTitleAsync(int listingId, string userId, string newTitle);
    Task<bool> ModifyListingImageAsync(int listingId, string userId, IFormFile newImage);
    Task<bool> ModifyListingLocationsAsync(int listingId, string userId, List<string> newLocations);
    Task<bool> ModifyListingDescriptionAsync(int listingId, string userId, string newAboutLesson, string newAboutYou);
    Task<bool> ModifyListingCategoryAsync(int listingId, string userId, int newCategoryId);
    Task<bool> ModifyListingRatesAsync(int listingId, string userId, RatesDto newRates);
    Task<bool> ToggleListingVisibilityAsync(int id);

    // Delete
    Task<bool> DeleteListingAsync(int listingId, string userId);
}

