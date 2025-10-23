using Ardalis.Specification;
using Avancira.Domain.Tutors;

namespace Avancira.Application.TutorProfiles.Specifications;

public class TutorProfileByUserIdSpec : Specification<TutorProfile>, ISingleResultSpecification<TutorProfile>
{
    public TutorProfileByUserIdSpec(string userId)
    {
        Query
            .Where(profile => profile.Id == userId)
            .Include(profile => profile.Subjects)
                .ThenInclude(subject => subject.Subject)
            .Include(profile => profile.Availabilities);
    }
}
