using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Payments.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Billing
{
    public interface IPaymentGateway
    {
        // Create
        Task<PaymentResult> CreatePaymentAsync(decimal amount, string currency, string returnUrl, string cancelUrl);
        Task<string> CreatePayoutAsync(string recipientAccountId, decimal amount, string currency);

        // Update
        Task<PaymentResult> CapturePaymentAsync(string paymentId, string stripeCustomerId, string cardId, decimal amount, string description);

        // Delete / Refund
        Task<string> RefundPaymentAsync(string paymentId, decimal amount);
    }
}
