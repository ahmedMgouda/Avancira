using Avancira.Domain.Auditing;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Avancira.Domain.Common.Contracts;
using Avancira.Domain.Geography;
using Avancira.Domain.Lessons;
using Avancira.Domain.Messaging;
using Avancira.Domain.Notifications;
using Avancira.Domain.PromoCodes;
using Avancira.Domain.Reviews;
using Avancira.Domain.Students;
using Avancira.Domain.Subjects;
using Avancira.Domain.Subscription;
using Avancira.Domain.Transactions;
using Avancira.Domain.Tutors;
using Avancira.Domain.UserCard;
using Avancira.Domain.UserSessions;
using Avancira.Domain.Wallets;
using Avancira.Infrastructure.Catalog;

namespace Avancira.Infrastructure.Persistence;
public class AvanciraDbContext : IdentityDbContext<
    User,
    Role,
    string,
    IdentityUserClaim<string>,
    IdentityUserRole<string>,
    IdentityUserLogin<string>,
    RoleClaim,
    IdentityUserToken<string>>
{
    private readonly IPublisher _publisher;

    public AvanciraDbContext(DbContextOptions options, IPublisher publisher)
        : base(options)
    {
        _publisher = publisher;
    }

    public DbSet<AuditTrail> AuditTrails { get; set; }
    public DbSet<SubjectCategory> SubjectCategories { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<TutorProfile> TutorProfiles { get; set; }
    public DbSet<TutorSubject> TutorSubjects { get; set; }
    public DbSet<TutorAvailability> TutorAvailabilities { get; set; }
    public DbSet<StudentProfile> StudentProfiles { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<LessonMaterial> LessonMaterials { get; set; }
    public DbSet<StudentReview> StudentReviews { get; set; }

    public DbSet<Referral> Referrals { get; set; }

    public DbSet<Chat> Chats { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<SubscriptionHistory> SubscriptionHistories { get; set; }
    public DbSet<PromoCode> PromoCodes { get; set; }
    public DbSet<UserCard> UserCards { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<WalletLog> WalletLogs { get; set; }

    public DbSet<UserSession> Sessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply soft delete global filter
        modelBuilder.AppendGlobalQueryFilter<ISoftDeletable>(s => s.Deleted == null);

        base.OnModelCreating(modelBuilder);

        modelBuilder.UseOpenIddict();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AvanciraDbContext).Assembly);
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
