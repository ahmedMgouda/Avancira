using Avancira.Domain.Common;
using Avancira.Domain.Transactions.Events;

namespace Avancira.Domain.Transactions
{
    public class Transaction : AuditableEntity
    {
        public string SenderId { get; private set; }
        public string? RecipientId { get; private set; }
        public decimal Amount { get; private set; }
        public decimal PlatformFee { get; private set; }
        public DateTime TransactionDate { get; private set; }
        public TransactionPaymentMethod PaymentMethod { get; private set; }
        public TransactionPaymentType PaymentType { get; private set; }
        public TransactionStatus Status { get; private set; }
        public bool IsRefunded { get; private set; }
        public DateTime? RefundedAt { get; private set; }
        public decimal? RefundAmount { get; private set; }
        public string Description { get; private set; }
        public string? PayPalPaymentId { get; private set; }
        public string? StripeCustomerId { get; private set; }
        public string? StripeCardId { get; private set; }

        public Transaction(string senderId, decimal amount, decimal platformFee,
            TransactionPaymentMethod paymentMethod, TransactionPaymentType paymentType, string description)
        {
            SenderId = senderId;
            Amount = amount;
            PlatformFee = platformFee;
            PaymentMethod = paymentMethod;
            PaymentType = paymentType;
            Description = description;
            TransactionDate = DateTime.UtcNow;
            Status = TransactionStatus.Created;

            QueueDomainEvent(new TransactionCreatedEvent(this));
        }

        public void ProcessRefund(decimal refundAmount)
        {
            RefundAmount = refundAmount;
            IsRefunded = true;
            RefundedAt = DateTime.UtcNow;

            QueueDomainEvent(new RefundProcessedEvent(this));
        }

        public void UpdateStatus(TransactionStatus newStatus)
        {
            var oldStatus = Status;
            Status = newStatus;

            if (oldStatus != newStatus)
            {
                QueueDomainEvent(new TransactionStatusChangedEvent(this, oldStatus, newStatus));
            }
        }
        public void AssignRecipient(string recipientId)
        {
            if (!string.IsNullOrWhiteSpace(recipientId))
            {
                RecipientId = recipientId;
            }
        }

        public void AssignStripeCustomer(string stripeCustomerId)
        {
            if (!string.IsNullOrWhiteSpace(stripeCustomerId))
            {
                StripeCustomerId = stripeCustomerId;
            }
        }

        public void AssignPayPalPaymentId(string payPalPaymentId)
        {
            if (!string.IsNullOrWhiteSpace(payPalPaymentId))
            {
                PayPalPaymentId = payPalPaymentId;
            }
        }

    }
}
