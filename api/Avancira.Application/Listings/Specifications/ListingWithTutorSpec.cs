using Ardalis.Specification;
using Avancira.Domain.Tutors;

namespace Avancira.Application.Listings.Specifications;

public class ListingWithTutorSpec : Specification<Listing>, ISingleResultSpecification<Listing>
{
    public ListingWithTutorSpec(int listingId)
    {
        Query
            .Where(listing => listing.Id == listingId)
            .Include(listing => listing.Tutor)
            .Include(listing => listing.Subject);
    }
}
