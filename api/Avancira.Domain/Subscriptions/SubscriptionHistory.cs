using Avancira.Domain.Catalog.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Domain.Subscription
{
    public class SubscriptionHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubscriptionId { get; set; }

        [ForeignKey(nameof(SubscriptionId))]
        public virtual Subscription? Subscription { get; set; }

        [Required]
        public SubscriptionBillingFrequency BillingFrequency { get; set; }

        public DateTime StartDate { get; set; } // When this record became active

        public DateTime? CancellationDate { get; set; } // If cancelled

        public DateTime NextBillingDate { get; set; } // Snapshot of what was next at this time

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // Charge at that time

        public DateTime ChangeDate { get; set; } // When this record was created
        public SubscriptionStatus Status
        {
            get
            {
                if (NextBillingDate < DateTime.UtcNow) return SubscriptionStatus.Expired;
                if (CancellationDate.HasValue && CancellationDate <= DateTime.UtcNow) return SubscriptionStatus.Cancelled;
                return SubscriptionStatus.Active;
            }
        }
        public override string ToString()
        {
            return $"SubscriptionHistory: {Id}, SubId: {SubscriptionId}, BillingFrequency: {BillingFrequency}, NextCharge: {NextBillingDate}, ChangeDate: {ChangeDate}";
        }
    }
}
