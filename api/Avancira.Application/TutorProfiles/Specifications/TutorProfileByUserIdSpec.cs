using Ardalis.Specification;
using Avancira.Domain.Tutors;

namespace Avancira.Application.TutorProfiles.Specifications;

public sealed class TutorProfileByUserIdSpec : Specification<TutorProfile>, ISingleResultSpecification<TutorProfile>
{
    public TutorProfileByUserIdSpec(string userId)
    {
        Query
            .Where(profile => profile.UserId == userId)
            .Include(profile => profile.Listings)
                .ThenInclude(listing => listing.Subject)
            .Include(profile => profile.Availabilities);
    }
}
