using Avancira.Domain.Common.Events;

namespace Avancira.Domain.UserSessions.Events
{
    public record SessionMetadataUpdatedEvent(Guid SessionId, string UserId) : DomainEvent;
}
