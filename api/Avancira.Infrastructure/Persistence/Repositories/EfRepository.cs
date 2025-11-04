using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Avancira.Application.Persistence;
using Avancira.Domain.Common.Contracts;
using Mapster;

namespace Avancira.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic EF Core repository supporting full CRUD operations with
/// automatic Mapster projection if no selector is provided.
/// </summary>
internal sealed class EfRepository<T> : RepositoryBase<T>, IRepository<T>, IReadRepository<T>
    where T : class, IAggregateRoot
{
    public EfRepository(AvanciraDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <summary>
    /// Applies the given specification to the query.
    /// If no custom selector is defined, applies Mapster's ProjectToType to map results.
    /// </summary>
    protected override IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> specification) =>
        specification.Selector is not null
            ? base.ApplySpecification(specification)
            : ApplySpecification(specification, false).ProjectToType<TResult>();
}
