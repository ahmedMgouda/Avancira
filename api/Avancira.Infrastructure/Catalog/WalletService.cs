using Avancira.Application.Payments;
using Avancira.Domain.Wallets;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class WalletService : IWalletService
    {
        private readonly AvanciraDbContext _dbContext;
        private readonly ILogger<WalletService> _logger;

        public WalletService(
            AvanciraDbContext dbContext,
            ILogger<WalletService> logger
        )
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        //public async Task<(string PayPalPaymentId, string ApprovalUrl, int TransactionId)> AddMoneyToWallet(string userId, PaymentRequestDto request)
        //{
        //    var user = await _dbContext.Users.FindAsync(userId);
        //    if (user == null) throw new KeyNotFoundException("User not found.");

        //    if (request.Amount <= 0)
        //        throw new ArgumentException("Amount must be greater than zero.");

        //    if (string.IsNullOrEmpty(user.StripeCustomerId))
        //        throw new ArgumentException("Stripe Customer ID is missing for the user.");

        //    // Use PaymentService to process transaction
        //    var transaction = await _paymentService.ProcessTransactionAsync(
        //        stripeCustomerId: user.StripeCustomerId,
        //        senderId: userId,
        //        recipientId: null, // Platform
        //        amount: request.Amount,
        //        paymentType: PaymentType.WalletTopUp,
        //        gatewayName: request.Gateway
        //    );

        //    return (transaction?.PayPalPaymentId ?? string.Empty, request.ReturnUrl, transaction?.Id ?? 0);
        //}

        public async Task<(decimal Balance, DateTime LastUpdated)> GetWalletBalanceAsync(string userId)
        {
            var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
                throw new InvalidOperationException("Wallet not found.");

            return (wallet.Balance, wallet.UpdatedAt);
        }

        public async Task ModifyWalletBalanceAsync(string userId, decimal amount, string reason)
        {
            var wallet = await _dbContext.Wallets.AsTracking().FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet != null)
            {
                wallet.Balance += amount;
                wallet.UpdatedAt = DateTime.UtcNow;

                // Log the wallet update
                _dbContext.WalletLogs.Add(new WalletLog
                {
                    WalletId = wallet.Id,
                    AmountChanged = amount,
                    NewBalance = wallet.Balance,
                    Reason = reason,
                    CreatedAt = DateTime.UtcNow
                });

                _dbContext.Wallets.Update(wallet);
            }
            else
            {
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = amount,
                    UpdatedAt = DateTime.UtcNow
                };

                await _dbContext.Wallets.AddAsync(wallet);
                await _dbContext.SaveChangesAsync();

                // Log the wallet creation
                _dbContext.WalletLogs.Add(new WalletLog
                {
                    WalletId = wallet.Id,
                    AmountChanged = amount,
                    NewBalance = wallet.Balance,
                    Reason = $"Wallet created with initial balance: {amount:C}"
                });
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
