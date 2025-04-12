using Avancira.Domain.Common.Events;

namespace Avancira.Domain.Messaging.Events
{
    public record MessageCreatedEvent(Message message) : DomainEvent;
    public record MessageDeletedEvent(Message message) : DomainEvent;
    public record MessageReadEvent(Message message) : DomainEvent;
    public record MessageDeliveredEvent(Message message) : DomainEvent;
    public record MessagePinnedEvent(Message Message) : DomainEvent;
    public record MessageUnpinnedEvent(Message Message) : DomainEvent;
}
