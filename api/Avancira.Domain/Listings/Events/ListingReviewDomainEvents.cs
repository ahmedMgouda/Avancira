using Avancira.Domain.Catalog;
using Avancira.Domain.Common.Events;

namespace Avancira.Domain.Listings.Events
{
    public record ListingReviewCreatedEvent(ListingReview ListingReview) : DomainEvent;

    public record ListingReviewUpdatedEvent(ListingReview ListingReview) : DomainEvent;
}
