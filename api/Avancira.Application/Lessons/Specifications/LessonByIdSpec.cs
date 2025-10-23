using Ardalis.Specification;
using Avancira.Domain.Lessons;

namespace Avancira.Application.Lessons.Specifications;

public class LessonByIdSpec : Specification<Lesson>, ISingleResultSpecification<Lesson>
{
    public LessonByIdSpec(int id)
    {
        Query
            .Where(lesson => lesson.Id == id)
            .Include(lesson => lesson.Listing)
                .ThenInclude(listing => listing.Subject)
            .Include(lesson => lesson.Tutor)
            .Include(lesson => lesson.Student)
            .Include(lesson => lesson.Review);
    }
}
