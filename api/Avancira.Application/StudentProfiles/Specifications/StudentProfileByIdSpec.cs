using Ardalis.Specification;
using Avancira.Domain.Students;

namespace Avancira.Application.StudentProfiles.Specifications
{
    public sealed class StudentProfileByIdSpec : Specification<StudentProfile>, ISingleResultSpecification<StudentProfile>
    {
        public StudentProfileByIdSpec(string userId)
        {
            Query
                .Where(profile => profile.UserId == userId)
                .Include(profile => profile.Lessons)
                .Include(profile => profile.Reviews);
        }
    }
}
