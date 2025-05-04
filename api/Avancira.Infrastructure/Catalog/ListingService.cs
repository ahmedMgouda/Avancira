using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class ListingService : IListingService
    {
        public Task<ListingResponseDto> CreateListingAsync(ListingRequestDto model, string userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteListingAsync(int listingId, string userId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ListingDto> GetLandingPageListings()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ListingDto> GetLandingPageTrendingListings()
        {
            throw new NotImplementedException();
        }

        public ListingDto GetListingById(Guid id)
        {
            throw new NotImplementedException();
        }

        public ListingStatisticsDto GetListingStatistics()
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<ListingResponseDto>> GetTutorListingsAsync(string userId, int page, int pageSize)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ModifyListingCategoryAsync(int listingId, string userId, int newCategoryId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ModifyListingDescriptionAsync(int listingId, string userId, string newAboutLesson, string newAboutYou)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ModifyListingImageAsync(int listingId, string userId, IFormFile newImage)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ModifyListingLocationsAsync(int listingId, string userId, List<string> newLocations)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ModifyListingRatesAsync(int listingId, string userId, RatesDto newRates)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ModifyListingTitleAsync(int listingId, string userId, string newTitle)
        {
            throw new NotImplementedException();
        }

        public PagedResult<ListingDto> SearchListings(string query, List<string> categories, int page, int pageSize, double? lat = null, double? lng = null, double radiusKm = 10)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ToggleListingVisibilityAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<ListingResponseDto> UpdateListingAsync(ListingRequestDto model, string userId)
        {
            throw new NotImplementedException();
        }
    }
}
