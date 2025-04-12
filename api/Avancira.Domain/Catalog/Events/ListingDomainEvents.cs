using Avancira.Domain.Common.Events;

namespace Avancira.Domain.Catalog.Events
{
    public record ListingCreatedEvent(Listing Listing) : DomainEvent;

    public record ListingUpdatedEvent(Listing Listing) : DomainEvent;

    public record ListingApprovedEvent(Listing Listing) : DomainEvent;

    public record ListingRejectedEvent(Listing Listing) : DomainEvent;

    public record ListingActivatedEvent(Listing Listing) : DomainEvent;

    public record ListingDeactivatedEvent(Listing Listing) : DomainEvent;
}
