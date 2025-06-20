using Avancira.Domain.Catalog.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog.Dtos
{
    public class SubscriptionDetailsDto
    {
        public string Plan { get { return BillingFrequency.ToString(); } }
        public SubscriptionBillingFrequency BillingFrequency { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime NextBillingDate { get; set; }
        public decimal NextBillingAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public List<SubscriptionHistoryDto> SubscriptionHistory { get; set; } = new();
    }
}
