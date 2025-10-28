using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Avancira.Application.Persistence;
using Avancira.Domain.Common.Contracts;

namespace Avancira.Infrastructure.Persistence.Repositories;

/// <summary>
/// Read-only EF Core repository for query operations using specifications.
/// </summary>
internal sealed class EfReadRepository<T> : RepositoryBase<T>, IReadRepository<T>
    where T : class, IAggregateRoot
{
    public EfReadRepository(AvanciraDbContext dbContext)
        : base(dbContext)
    {
    }
}
