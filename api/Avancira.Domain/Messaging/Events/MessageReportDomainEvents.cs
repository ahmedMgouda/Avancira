using Avancira.Domain.Messagings;
using Avancira.Domain.Common.Events;

namespace Avancira.Domain.Messaging.Events
{
    public record MessageReportedEvent(MessageReport MessageReport) : DomainEvent;
}
