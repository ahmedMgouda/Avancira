using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Catalog.Events;
using Avancira.Domain.Common;
using Backend.Domain.PromoCodes;

namespace Avancira.Domain.Catalog;
public class Listing : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public decimal HourlyRate { get; private set; }
    public ListingLocationType LocationType { get; private set; } = ListingLocationType.Webcam;

    public bool DisplayOnLandingPage { get; private set; } = false;
    public bool IsActive { get; private set; } = true; // User can deactivate the listing, but it will still be visible in the admin panel.

    public ListingApprovalStatus ApprovalStatus { get; private set; }

    public string? ReviewFeedback { get; private set; }
    public string? AdminReviewerId { get; private set; }

    public DateTime? ReviewDate { get; private set; }

    public virtual ICollection<ListingCategory> ListingCategories { get; set; }
    public virtual ICollection<ListingReview> ListingReviews { get; private set; }
    public virtual ICollection<ListingPromoCode> ListingPromoCodes { get; set; }

    public bool IsVisible { get; private set; } = true; // User can hide the listing to stop students from seeing it to have a pause or holiday.
    public string UserId { get; set; }

    public decimal AverageRating => ListingReviews.Any() ? ListingReviews.Average(r => r.RatingValue) : 0;

    public Listing()
    {
        ListingCategories = new List<ListingCategory>();
        ListingReviews = new List<ListingReview>();
        ListingPromoCodes = new List<ListingPromoCode>();
    }

    public static Listing Create(string name, string description, decimal hourlyRate, ListingLocationType locationType, string createdById)
    {
        var listing = new Listing
        {
            Name = name,
            Description = description,
            HourlyRate = hourlyRate,
            LocationType = locationType,
            ApprovalStatus = ListingApprovalStatus.Pending,
            UserId = createdById
        };

        listing.QueueDomainEvent(new ListingCreatedEvent(listing));

        return listing;
    }

    public void Update(string? name, string? description, decimal? hourlyRate, ListingLocationType? locationType)
    {
        bool isUpdated = false;

        if (!string.IsNullOrWhiteSpace(name) && !string.Equals(Name, name, StringComparison.OrdinalIgnoreCase))
        {
            Name = name;
            isUpdated = true;
        }

        if (!string.Equals(Description, description, StringComparison.OrdinalIgnoreCase))
        {
            Description = description;
            isUpdated = true;
        }

        if (hourlyRate.HasValue && HourlyRate != hourlyRate.Value)
        {
            HourlyRate = hourlyRate.Value;
            isUpdated = true;
        }

        if (locationType.HasValue && LocationType != locationType.Value)
        {
            LocationType = locationType.Value;
            isUpdated = true;
        }

        if (isUpdated)
        {
            QueueDomainEvent(new ListingUpdatedEvent(this));
        }
    }

    public void ApproveListing(string adminReviewerId)
    {
        ApprovalStatus = ListingApprovalStatus.Approved;
        AdminReviewerId = adminReviewerId;
        ReviewDate = DateTime.UtcNow;

        QueueDomainEvent(new ListingApprovedEvent(this));
    }
    public void RejectListing(string reviewFeedback, string adminReviewerId)
    {
        ApprovalStatus = ListingApprovalStatus.Rejected;
        ReviewFeedback = reviewFeedback;
        AdminReviewerId = adminReviewerId;
        ReviewDate = DateTime.UtcNow;

        QueueDomainEvent(new ListingRejectedEvent(this));
    }
    public void ToggleActivation()
    {
        IsActive = !IsActive;
        QueueDomainEvent(IsActive ? new ListingActivatedEvent(this) : new ListingDeactivatedEvent(this));
    }
    public void ToggleDisplayOnHomePage()
    {
        DisplayOnLandingPage = !DisplayOnLandingPage;
    }

    public void UpdateDescription(string newDescription)
    {
        if (!string.IsNullOrWhiteSpace(newDescription) && !string.Equals(Description, newDescription, StringComparison.OrdinalIgnoreCase))
        {
            Description = newDescription;
            QueueDomainEvent(new ListingUpdatedEvent(this));
        }
    }

    public void UpdateLocations(ListingLocationType newLocations)
    {
        if (LocationType != newLocations)
        {
            LocationType = newLocations;
            QueueDomainEvent(new ListingUpdatedEvent(this));
        }
    }
    public void UpdateHourlyRate(decimal newHourlyRate)
    {
        if (newHourlyRate > 0 && HourlyRate != newHourlyRate)
        {
            HourlyRate = newHourlyRate;
            QueueDomainEvent(new ListingUpdatedEvent(this));
        }
    }
    public void ToggleVisibility()
    {
        IsVisible = !IsVisible;
        QueueDomainEvent(new ListingUpdatedEvent(this));
    }

    public void UpdateTitle(string newTitle)
    {
        if (!string.IsNullOrWhiteSpace(newTitle) && !string.Equals(Name, newTitle, StringComparison.OrdinalIgnoreCase))
        {
            Name = newTitle;
            QueueDomainEvent(new ListingUpdatedEvent(this));
        }
    }

    public void Delete()
    {
        if (IsActive)
        {
            IsActive = false;
            QueueDomainEvent(new ListingDeactivatedEvent(this));
        }
    }
}
