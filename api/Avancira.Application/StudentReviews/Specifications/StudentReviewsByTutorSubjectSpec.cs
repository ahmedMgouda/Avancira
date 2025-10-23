using Ardalis.Specification;
using Avancira.Domain.Reviews;

namespace Avancira.Application.StudentReviews.Specifications;

public class StudentReviewsByTutorSubjectSpec : Specification<StudentReview>
{
    public StudentReviewsByTutorSubjectSpec(int tutorSubjectId)
    {
        Query
            .Where(review => review.Lesson.TutorSubjectId == tutorSubjectId);
    }
}
