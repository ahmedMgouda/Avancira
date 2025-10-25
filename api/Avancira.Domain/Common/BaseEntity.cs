using Avancira.Domain.Common.Contracts;
using Avancira.Domain.Common.Events;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Avancira.Domain.Common;
public abstract class BaseEntity<TId> : IEntity<TId>
{
    public TId Id { get; protected set; } = default!;
    [NotMapped]
    public Collection<DomainEvent> DomainEvents { get; } = new Collection<DomainEvent>();
    public void QueueDomainEvent(DomainEvent @event)
    {
        if (!DomainEvents.Contains(@event))
            DomainEvents.Add(@event);
    }
}

public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity() => Id = Guid.NewGuid();
}
