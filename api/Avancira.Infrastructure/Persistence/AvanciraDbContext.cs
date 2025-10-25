using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Domain.Auditing;
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
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Avancira.Infrastructure.Persistence;

public sealed class AvanciraDbContext : IdentityDbContext<
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

    public AvanciraDbContext(
        DbContextOptions<AvanciraDbContext> options,
        IPublisher publisher)
        : base(options)
    {
        _publisher = publisher;
    }

    // ============================================================
    // DbSets
    // ============================================================
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();
    public DbSet<SubjectCategory> SubjectCategories => Set<SubjectCategory>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<TutorProfile> TutorProfiles => Set<TutorProfile>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<TutorAvailability> TutorAvailabilities => Set<TutorAvailability>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonMaterial> LessonMaterials => Set<LessonMaterial>();
    public DbSet<StudentReview> StudentReviews => Set<StudentReview>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionHistory> SubscriptionHistories => Set<SubscriptionHistory>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<UserCard> UserCards => Set<UserCard>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletLog> WalletLogs => Set<WalletLog>();
    public DbSet<UserSession> Sessions => Set<UserSession>();

    // ============================================================
    // Model configuration
    // ============================================================
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Always call base first for Identity setup
        base.OnModelCreating(modelBuilder);

        // OpenIddict support
        modelBuilder.UseOpenIddict();

        // Apply all IEntityTypeConfiguration<T> in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AvanciraDbContext).Assembly);

        // Soft-delete global query filter
        modelBuilder.AppendGlobalQueryFilter<ISoftDeletable>(e => e.Deleted == null);
    }

    // ============================================================
    // SaveChanges + Domain Events
    // ============================================================
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
                var events = e.DomainEvents.ToList();
                e.DomainEvents.Clear();
                return events;
            })
            .ToList();

        foreach (var domainEvent in domainEvents)
            await _publisher.Publish(domainEvent).ConfigureAwait(false);
    }
}
