using Ardalis.Specification;
using Avancira.Domain.Reviews;

namespace Avancira.Application.StudentReviews.Specifications;

public class StudentReviewsByListingSpec : Specification<StudentReview>
{
    public StudentReviewsByListingSpec(int listingId)
    {
        Query
            .Where(review => review.Lesson.ListingId == listingId);
    }
}
