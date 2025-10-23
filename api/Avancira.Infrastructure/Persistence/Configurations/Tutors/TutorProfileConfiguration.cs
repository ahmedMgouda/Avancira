using Avancira.Domain.Tutors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Tutors;

public class TutorProfileConfiguration : IEntityTypeConfiguration<TutorProfile>
{
    public void Configure(EntityTypeBuilder<TutorProfile> builder)
    {
        builder.ToTable("TutorProfiles");

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Id)
            .HasColumnName("UserId")
            .HasMaxLength(450);

        builder.Property(profile => profile.Headline)
            .HasMaxLength(200);

        builder.Property(profile => profile.Description)
            .HasMaxLength(2000);

        builder.Property(profile => profile.TeachingPhilosophy)
            .HasMaxLength(500);

        builder.Property(profile => profile.Specializations)
            .HasMaxLength(500);

        builder.Property(profile => profile.IntroVideoUrl)
            .HasMaxLength(500);

        builder.Property(profile => profile.Languages)
            .HasMaxLength(200);

        builder.HasMany(profile => profile.Listings)
            .WithOne(listing => listing.Tutor)
            .HasForeignKey(listing => listing.TutorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(profile => profile.Availabilities)
            .WithOne(availability => availability.Tutor)
            .HasForeignKey(availability => availability.TutorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
