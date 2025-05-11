using Avancira.Domain.Catalog.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Domain.Subscriptions
{
    public class Subscription
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        public DateTime NextBillingDate { get; set; } // Always the next scheduled charge
        public DateTime? CancellationDate { get; set; } // Only set when cancelled
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be a positive value.")]
        public decimal Amount { get; set; }
        [Required]
        public SubscriptionBillingFrequency BillingFrequency { get; set; }
        [NotMapped]
        public SubscriptionStatus Status
        {
            get
            {
                if (NextBillingDate < DateTime.UtcNow) return SubscriptionStatus.Expired;
                if (CancellationDate.HasValue && CancellationDate <= DateTime.UtcNow) return SubscriptionStatus.Cancelled;
                return SubscriptionStatus.Active;
            }
        }

        public Subscription()
        {
            UserId = string.Empty;
        }

        public override string ToString()
        {
            return $"Subscription: {Id}, UserId: {UserId}, Amount: {Amount:C}, StartDate: {StartDate}, EndDate: {NextBillingDate}, Status: {Status}";
        }
    }
}
