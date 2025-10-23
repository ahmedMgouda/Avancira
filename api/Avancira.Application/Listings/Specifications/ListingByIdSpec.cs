using Ardalis.Specification;
using Avancira.Domain.Tutors;

namespace Avancira.Application.Listings.Specifications;

public class ListingByIdSpec : Specification<Listing>, ISingleResultSpecification<Listing>
{
    public ListingByIdSpec(int id)
    {
        Query
            .Where(listing => listing.Id == id)
            .Include(listing => listing.Subject)
            .Include(listing => listing.Tutor);
    }
}
