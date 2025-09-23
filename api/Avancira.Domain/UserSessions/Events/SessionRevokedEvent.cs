using Avancira.Domain.Common.Events;

namespace Avancira.Domain.UserSessions.Events
{
    public record SessionRevokedEvent(Guid SessionId, string UserId, string? Reason) : DomainEvent;
}
