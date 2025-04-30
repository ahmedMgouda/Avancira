using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog.Dtos
{
    public class SubscriptionHistoryDto
    {
        public string Action { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ChangeDate { get; set; }
        public string BillingFrequency { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
