using Avancira.Domain.Common.Events;
using Avancira.Domain.UserSessions.ValueObjects;

namespace Avancira.Domain.UserSessions.Events
{
    public record SessionCreatedEvent(Guid SessionId, string UserId, DeviceCategory DeviceCategory) : DomainEvent;

}
