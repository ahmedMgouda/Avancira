using Ardalis.Specification;
using Avancira.Domain.Tutors;

namespace Avancira.Application.Listings.Specifications;

public class ListingsByTutorSpec : Specification<Listing>
{
    public ListingsByTutorSpec(string tutorId)
    {
        Query
            .Where(listing => listing.TutorId == tutorId)
            .Include(listing => listing.Subject);
    }
}
