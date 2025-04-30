using Avancira.Application.Catalog.Dtos;
using Avancira.Domain.Transactions;
using System.Threading.Tasks;

public interface IPaymentService
{
    // Create
    Task<PaymentResult> CreatePaymentAsync(PaymentRequestDto request);
    Task<string> CreatePayoutAsync(string sellerId, decimal amount, string currency, string gatewayName = "Stripe");


    // Read
    Task<PaymentHistoryDto> GetPaymentHistoryAsync(string userId);

    // Update
    Task<Transaction> CapturePaymentAsync(int transactionId, string gatewayName = "Stripe", string recipientId = "");

    // Refunds & Deletions
    Task<bool> RefundPaymentAsync(int transactionId, decimal refundAmount, decimal retainedAmount, string gatewayName = "Stripe");
}

