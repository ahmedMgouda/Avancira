using Avancira.Domain.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog.Dtos
{
    public class SubscriptionRequestDto
    {
        public string? PayPalPaymentId { get; set; }
        public decimal? Amount { get; set; }
        public TransactionPaymentMethod PaymentMethod { get; set; }
        public TransactionPaymentType PaymentType { get; set; }
        public string BillingFrequency { get; set; }
        public string? PromoCode { get; set; }

        public SubscriptionRequestDto()
        {
            BillingFrequency = string.Empty;
        }
    }
}
