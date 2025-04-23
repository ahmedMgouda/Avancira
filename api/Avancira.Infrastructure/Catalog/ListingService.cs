using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class ListingService : IListingService
    {
        public IEnumerable<ListingDto> GetLandingPageListings()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ListingDto> GetLandingPageTrendingListings()
        {
            throw new NotImplementedException();
        }

        public ListingStatisticsDto GetListingStatistics()
        {
            throw new NotImplementedException();
        }
    }
}
