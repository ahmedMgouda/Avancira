using Ardalis.Specification.EntityFrameworkCore;
using Avancira.Application.Persistence;
using Avancira.Domain.Common.Contracts;

namespace Avancira.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic EF Core repository supporting full CRUD operations with specifications.
/// </summary>
internal sealed class EfRepository<T> : RepositoryBase<T>, IRepository<T>
    where T : class, IAggregateRoot
{
    public EfRepository(AvanciraDbContext dbContext)
        : base(dbContext)
    {
    }
}
