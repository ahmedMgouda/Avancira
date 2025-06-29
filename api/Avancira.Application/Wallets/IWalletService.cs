using System;
using System.Threading.Tasks;

namespace Avancira.Application.Payments;

public interface IWalletService
{
    // Create
    // Task<(string PayPalPaymentId, string ApprovalUrl, int TransactionId)> AddMoneyToWallet(string userId, PaymentRequestDto request);
    // Read
    Task<(decimal Balance, DateTime LastUpdated)> GetWalletBalanceAsync(string userId);
    // Update
    Task ModifyWalletBalanceAsync(string userId, decimal amount, string reason);
}
