using Ardalis.Specification;
using Avancira.Domain.Tutors;

namespace Avancira.Application.TutorSubjects.Specifications;

public class TutorSubjectsByTutorSpec : Specification<TutorSubject>
{
    public TutorSubjectsByTutorSpec(string tutorId)
    {
        Query
            .Where(subject => subject.TutorId == tutorId)
            .Include(subject => subject.Subject);
    }
}
