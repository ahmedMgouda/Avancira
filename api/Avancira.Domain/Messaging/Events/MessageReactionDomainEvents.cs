using Avancira.Domain.Common.Events;

namespace Avancira.Domain.Messaging.Events
{
    public record MessageReactionAddedEvent(MessageReaction MessageReaction) : DomainEvent;
    public record MessageReactionUpdatedEvent(MessageReaction MessageReaction) : DomainEvent;
    public record MessageReactionRemovedEvent(MessageReaction MessageReaction) : DomainEvent;

    
}
