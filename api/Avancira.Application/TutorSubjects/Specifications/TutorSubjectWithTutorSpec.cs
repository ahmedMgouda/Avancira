using Ardalis.Specification;
using Avancira.Domain.Tutors;

namespace Avancira.Application.TutorSubjects.Specifications;

public class TutorSubjectWithTutorSpec : Specification<TutorSubject>, ISingleResultSpecification<TutorSubject>
{
    public TutorSubjectWithTutorSpec(int tutorSubjectId)
    {
        Query
            .Where(subject => subject.Id == tutorSubjectId)
            .Include(subject => subject.Tutor)
            .Include(subject => subject.Subject);
    }
}
