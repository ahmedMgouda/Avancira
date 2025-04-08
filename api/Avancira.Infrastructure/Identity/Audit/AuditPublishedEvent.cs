using Avancira.Domain.Auditing;
using MediatR;
using System.Collections.ObjectModel;

namespace Avancira.Infrastructure.Identity.Audit;
public class AuditPublishedEvent : INotification
{
    public AuditPublishedEvent(Collection<AuditTrail>? trails)
    {
        Trails = trails;
    }
    public Collection<AuditTrail>? Trails { get; }
}

