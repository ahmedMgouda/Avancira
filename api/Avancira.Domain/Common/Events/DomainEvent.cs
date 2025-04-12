using MediatR;

namespace Avancira.Domain.Common.Events;
public abstract record DomainEvent : IDomainEvent, INotification
{
    public DateTime RaisedOn { get; protected set; } = DateTime.UtcNow;
}
