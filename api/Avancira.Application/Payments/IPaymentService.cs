using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Payments.Dtos;
using Avancira.Domain.Transactions;
using System.Threading.Tasks;

namespace Avancira.Application.Payments;

public interface IPaymentService
{
    // Create
    Task<PaymentResult> CreatePaymentAsync(PaymentRequestDto request);
    Task<string> CreatePayoutAsync(string sellerId, decimal amount, string currency, string gatewayName = "Stripe");


    // Read
    Task<PaymentHistoryDto> GetPaymentHistoryAsync(string userId);

    // Update
    Task<Transaction> CapturePaymentAsync(Guid transactionId, string gatewayName = "Stripe", string recipientId = "");

    // Refunds & Deletions
    Task<bool> RefundPaymentAsync(Guid transactionId, decimal refundAmount, decimal retainedAmount, string gatewayName = "Stripe");
}
