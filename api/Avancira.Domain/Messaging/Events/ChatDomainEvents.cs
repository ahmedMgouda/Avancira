using Avancira.Domain.Common.Events;

namespace Avancira.Domain.Messaging.Events
{
    public record UserBlockedEvent(Chat Chat) : DomainEvent;
    public record UserUnblockedEvent(Chat Chat) : DomainEvent;

}
