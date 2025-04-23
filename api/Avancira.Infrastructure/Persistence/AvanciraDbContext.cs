using Avancira.Application.Persistence;
using Avancira.Domain.Auditing;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Reflection;
using Avancira.Domain.Common.Contracts;
using Avancira.Domain.Catalog;

namespace Avancira.Infrastructure.Persistence;
public class AvanciraDbContext : IdentityDbContext<User,
    Role, 
    string, 
    IdentityUserClaim<string>, 
    IdentityUserRole<string>, 
    IdentityUserLogin<string>, 
    RoleClaim, 
    IdentityUserToken<string>>
{
    private readonly IPublisher _publisher;
    private readonly DatabaseOptions _settings;

    public AvanciraDbContext(DbContextOptions options, IPublisher publisher, IOptions<DatabaseOptions> settings)
        : base(options)
    {
        _publisher = publisher;
        _settings = settings.Value;
    }

    public DbSet<AuditTrail> AuditTrails { get; set; }
    public DbSet<ListingCategory> LessonCategories { get; set; }
    public DbSet<Listing> Listings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply soft delete global filter
        modelBuilder.AppendGlobalQueryFilter<ISoftDeletable>(s => s.Deleted == null);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AvanciraDbContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();

        if (!optionsBuilder.IsConfigured && !string.IsNullOrWhiteSpace(_settings.ConnectionString))
        {
            optionsBuilder.ConfigureDatabase(_settings.Provider, _settings.ConnectionString);
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await PublishDomainEventsAsync().ConfigureAwait(false);
        return result;
    }

    private async Task PublishDomainEventsAsync()
    {
        var domainEvents = ChangeTracker.Entries<IEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .SelectMany(e =>
            {
                var domainEvents = e.DomainEvents.ToList();
                e.DomainEvents.Clear();
                return domainEvents;
            })
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent).ConfigureAwait(false);
        }
    }
}
