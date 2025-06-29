using Avancira.Domain.Catalog.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Payments.Dtos
{
    public class PaymentResult
    {
        public string? PaymentId { get; set; }
        public string? ApprovalUrl { get; set; } // For gateways like Stripe or PayPal
        public PaymentResultStatus Status { get; set; } // Optional: Add status information if needed
    }
}
