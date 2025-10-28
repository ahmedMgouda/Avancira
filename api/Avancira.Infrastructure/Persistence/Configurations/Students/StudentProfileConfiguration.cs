using Avancira.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Students;

public sealed class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
      
        builder.ToTable("StudentProfiles");

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Id)
            .HasColumnName("UserId")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(profile => profile.LearningGoal)
            .HasMaxLength(500);

        builder.Property(profile => profile.CurrentEducationLevel)
            .HasMaxLength(100);

        builder.Property(profile => profile.School)
            .HasMaxLength(100);

        builder.Property(profile => profile.Major)
            .HasMaxLength(100);

        builder.Property(profile => profile.SubscriptionStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

  
        builder.OwnsOne(profile => profile.SubscriptionPeriod, period =>
        {
            period.Property(p => p.StartUtc)
                .HasColumnName("SubscriptionStartUtc")
                .IsRequired();

            period.Property(p => p.EndUtc)
                .HasColumnName("SubscriptionEndUtc")
                .IsRequired();

            period.WithOwner();
        });

        builder.Property(profile => profile.CreatedOnUtc)
            .IsRequired();

        builder.Property(profile => profile.CanBook)
            .IsRequired();

        builder.Property(profile => profile.HasUsedTrialLesson)
            .IsRequired();

        builder.Property(profile => profile.IsComplete)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(profile => profile.ShowStudentProfileReminder)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
