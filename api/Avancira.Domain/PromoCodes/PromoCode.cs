using Avancira.Domain.Common;
using Avancira.Domain.PromoCodes.Events;

namespace Backend.Domain.PromoCodes;
public class PromoCode : AuditableEntity
{
    public PromoCode()
    {
        ListingPromoCodes = new List<ListingPromoCode>();
    }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public double DiscountPercentage { get; set; }
    public int MaxUsageCount { get; set; }
    public int UsageCount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public PromoCodeType Type { get; set; }  // Tutor or System promo code type

    public virtual ICollection<ListingPromoCode> ListingPromoCodes { get; set; }

    public bool IsValid()
    {
        return DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate && UsageCount < MaxUsageCount;
    }
    public void ToggleActive()
    {
        IsActive = !IsActive;
    }
    public void IncrementUsageCount()
    {
        if (UsageCount < MaxUsageCount)
        {
            UsageCount++;

            if (UsageCount == MaxUsageCount)
            {
                QueueDomainEvent(new PromoCodeMaxUsageReachedEvent(this));
            }
        }
    }
}


