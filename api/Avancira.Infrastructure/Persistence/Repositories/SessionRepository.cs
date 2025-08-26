using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Avancira.Application.Persistence;
using Avancira.Domain.Common.Contracts;
using Mapster;

namespace Avancira.Infrastructure.Persistence.Repositories;

internal sealed class SessionRepository<T> : RepositoryBase<T>, IReadRepository<T>, IRepository<T>
    where T : class, IAggregateRoot
{
    public SessionRepository(AvanciraDbContext context)
        : base(context)
    {
    }

    protected override IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> specification) =>
        specification.Selector is not null
            ? base.ApplySpecification(specification)
            : ApplySpecification(specification, false).ProjectToType<TResult>();
}
