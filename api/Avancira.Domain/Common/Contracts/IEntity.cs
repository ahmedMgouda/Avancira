using Avancira.Domain.Common.Events;
using System.Collections.ObjectModel;

namespace Avancira.Domain.Common.Contracts;
public interface IEntity
{
    Collection<DomainEvent> DomainEvents { get; }
}

public interface IEntity<out TId> : IEntity
{
    TId Id { get; }
}
