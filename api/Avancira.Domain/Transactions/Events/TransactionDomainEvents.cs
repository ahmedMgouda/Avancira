using Avancira.Domain.Common.Events;

namespace Avancira.Domain.Transactions.Events
{
    public record TransactionCreatedEvent(Transaction Transaction) : DomainEvent;

    public record TransactionStatusChangedEvent(Transaction Transaction, TransactionStatus OldStatus, TransactionStatus NewStatus) : DomainEvent;

    public record RefundProcessedEvent(Transaction Transaction) : DomainEvent;
}
