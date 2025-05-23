﻿using Avancira.Domain.Common.Events;

namespace Avancira.Domain.Catalog.Events
{
    public record ListingReviewCreatedEvent(ListingReview ListingReview) : DomainEvent;

    public record ListingReviewUpdatedEvent(ListingReview ListingReview) : DomainEvent;
}
