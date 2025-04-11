using Avancira.Domain.Catalog.Events;
using Avancira.Domain.Common;

namespace Avancira.Domain.Catalog;

public class ListingReview : AuditableEntity
{
    public Guid ListingId { get; private set; }
    public Listing Listing { get; private set; } = default!;
    public string StudentId { get; private set; } = string.Empty;
    public decimal RatingValue { get; private set; }
    public string? Comment { get; private set; }
    public DateTime RatingDate { get; private set; } = DateTime.UtcNow;

    private ListingReview() { }

    public ListingReview(Guid listingId, string studentId, decimal ratingValue, string? comment)
    {
        ListingId = listingId;
        StudentId = studentId;
        RatingValue = ratingValue;
        Comment = comment;
        RatingDate = DateTime.UtcNow;

        QueueDomainEvent(new ListingReviewCreatedEvent(this));
    }

    public ListingReview UpdateReview(decimal ratingValue, string? comment)
    {
        if (RatingValue != ratingValue || Comment != comment)
        {
            RatingValue = ratingValue;
            Comment = comment;

            QueueDomainEvent(new ListingReviewUpdatedEvent(this));
        }

        return this;
    }
}
