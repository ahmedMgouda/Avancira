using Ardalis.Specification;
using Avancira.Domain.Reviews;

namespace Avancira.Application.StudentReviews.Specifications;
public sealed class StudentReviewByLessonIdSpec : Specification<StudentReview>, ISingleResultSpecification<StudentReview>
{
    public StudentReviewByLessonIdSpec(int lessonId)
    {
        Query.Where(r => r.LessonId == lessonId);
    }
}
