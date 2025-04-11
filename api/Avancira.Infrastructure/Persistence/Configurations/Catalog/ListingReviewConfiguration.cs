using Avancira.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Catalog;

public class ListingReviewConfiguration : IEntityTypeConfiguration<ListingReview>
{
    public void Configure(EntityTypeBuilder<ListingReview> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RatingValue)
            .HasPrecision(3, 2)
            .IsRequired();

        builder.Property(r => r.Comment)
            .HasMaxLength(1000);

        builder.Property(r => r.RatingDate)
            .IsRequired();

        builder.Property(r => r.StudentId)
            .IsRequired();

        builder.HasOne(r => r.Listing)
            .WithMany(l => l.ListingReviews)
            .HasForeignKey(r => r.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
