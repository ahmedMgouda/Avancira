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

namespace Avancira.Infrastructure.Catalog
{
    public class ListingService : IListingService
    {
        private readonly AvanciraDbContext _dbContext;
        public ListingService(AvanciraDbContext dbContext)
        {
            _dbContext = dbContext;
        }

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
            var listings = _dbContext.Listings
                //.Include(l => l.ListingLessonCategories).ThenInclude(l => l.LessonCategory)
                //.Include(l => l.User)
                //.Where(l => l.Active && l.IsVisible)
                .OrderBy(_ => Guid.NewGuid())
                .Take(50)
                .ToList();

            return listings.Select(l => MapToListingDto(l));
        }

        public IEnumerable<ListingDto> GetLandingPageTrendingListings()
        {
            var listings = _dbContext.Listings
                //.Include(l => l.ListingLessonCategories).ThenInclude(l => l.LessonCategory)
                //.Include(l => l.User)
                //.Where(l => l.Active && l.IsVisible)
                .OrderBy(_ => Guid.NewGuid())
                .Take(10)
                .ToList();

            return listings.Select(l => MapToListingDto(l));
        }

        public ListingDto GetListingById(Guid id)
        {
            throw new NotImplementedException();
        }

        public ListingStatisticsDto GetListingStatistics()
        {
            var today = DateTime.UtcNow.Date;
            var startOfDay = today;
            var endOfDay = today.AddDays(1);

            var stats = _dbContext.Listings
                .AsNoTracking()
                //.Where(l => l.Active && l.IsVisible)
                .GroupBy(_ => 1)
                .Select(g => new ListingStatisticsDto
                {
                    TotalListings = g.Count(),
                    //NewListingsToday = g.Count(l => startOfDay <= l.CreatedAt && l.CreatedAt < endOfDay)
                })
                .FirstOrDefault() ?? new ListingStatisticsDto { TotalListings = 0, NewListingsToday = 0 };

            return stats;
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

        private ListingDto MapToListingDto(Listing listing, int? contactedCount = null)
        {
            //AddressDto addressDto = null;
            //var address = listing.User?.Address;
            //if (address != null)
            //{
            //    addressDto = new AddressDto
            //    {
            //        StreetAddress = address.StreetAddress,
            //        City = address.City,
            //        State = address.State,
            //        Country = address.Country,
            //        PostalCode = address.PostalCode,
            //        Latitude = address.Latitude,
            //        Longitude = address.Longitude,
            //        FormattedAddress = address.FormattedAddress
            //    };
            //}

            return new ListingDto
            {
                //Id = listing.Id,
                //IsVisible = listing.IsVisible,
                //TutorId = listing.User.Id,
                //TutorName = listing.User?.FullName,
                //TutorBio = listing.User?.Bio,
                ContactedCount = contactedCount ?? 0,
                //TutorAddress = addressDto,
                Reviews = 0,
                //LessonCategory = listing.ListingLessonCategories.FirstOrDefault()?.LessonCategory.Name,
                Title = listing.Name,
                //ListingImagePath = listing.User.ProfileImageUrl,
                //Locations = Enum.GetValues(typeof(ListingLocationType))
                //                .Cast<ListingLocationType>()
                //                .Where(location => (listing.Locations & location) == location && location != ListingLocationType.None)
                //                .Select(location => location.ToString())
                //                .ToList(),
                AboutLesson = listing.Description, // listing.AboutLesson,
                AboutYou = string.Empty, // listing.AboutYou,
                Rate = $"{listing.HourlyRate}/h",// $"{listing.Rates.Hourly}/h",
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
