using Ardalis.Specification;
using Avancira.Domain.Tutors;

namespace Avancira.Application.TutorSubjects.Specifications;

public class TutorSubjectByIdSpec : Specification<TutorSubject>, ISingleResultSpecification<TutorSubject>
{
    public TutorSubjectByIdSpec(int id)
    {
        Query
            .Where(subject => subject.Id == id)
            .Include(subject => subject.Subject)
            .Include(subject => subject.Tutor);
    }
}
