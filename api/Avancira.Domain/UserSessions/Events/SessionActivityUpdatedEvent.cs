using Avancira.Domain.Common.Events;

namespace Avancira.Domain.UserSessions.Events
{
    public record SessionActivityUpdatedEvent(Guid SessionId, string UserId) : DomainEvent;
}
