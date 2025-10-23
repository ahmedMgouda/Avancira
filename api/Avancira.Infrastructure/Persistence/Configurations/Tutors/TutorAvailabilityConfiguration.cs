using Avancira.Domain.Tutors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Tutors;

public class TutorAvailabilityConfiguration : IEntityTypeConfiguration<TutorAvailability>
{
    public void Configure(EntityTypeBuilder<TutorAvailability> builder)
    {
        builder.ToTable("TutorAvailabilities");

        builder.HasKey(availability => availability.Id);

        builder.Property(availability => availability.TutorId)
            .HasMaxLength(450)
            .IsRequired();

        builder.HasIndex(availability => new { availability.TutorId, availability.DayOfWeek, availability.StartTime })
            .HasDatabaseName("IX_TutorAvailability_Unique")
            .IsUnique();
    }
}
