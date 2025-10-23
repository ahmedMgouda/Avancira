using Avancira.Domain.Tutors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Tutors;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("Listings");

        builder.HasKey(listing => listing.Id);

        builder.Property(listing => listing.HourlyRate)
            .HasPrecision(10, 2);

        builder.Property(listing => listing.AdminComment)
            .HasMaxLength(500);

        builder.Property(listing => listing.CreatedOnUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(listing => new { listing.TutorId, listing.SubjectId })
            .IsUnique();
    }
}
