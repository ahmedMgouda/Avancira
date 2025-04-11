using Avancira.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Catalog
{
    public class ListingConfiguration : IEntityTypeConfiguration<Listing>
    {
        public void Configure(EntityTypeBuilder<Listing> builder)
        {
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Name)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(l => l.Description)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(l => l.HourlyRate)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(l => l.LocationType)
                .IsRequired();

            builder.Property(l => l.DisplayOnLandingPage)
                .IsRequired();

            builder.Property(l => l.IsActive)
                .IsRequired();

            builder.Property(l => l.ApprovalStatus)
                .IsRequired();

            builder.Property(l => l.ReviewFeedback)
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(l => l.AdminReviewerId)
                .IsRequired(false);

            builder.Property(l => l.ReviewDate)
                .IsRequired(false);

            builder.HasMany(l => l.ListingCategories)
                .WithOne(lc => lc.Listing)
                .HasForeignKey(lc => lc.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(l => l.ListingReviews)
                .WithOne(r => r.Listing)
                .HasForeignKey(r => r.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.ListingPromoCodes)
              .WithOne(lp => lp.Listing)
              .HasForeignKey(lp => lp.ListingId)
              .OnDelete(DeleteBehavior.Cascade);

            builder.Ignore(l => l.AverageRating);
        }
    }
}
