using Avancira.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Students;

public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("StudentProfiles");

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Id)
            .HasColumnName("UserId")
            .HasMaxLength(450);

        builder.Property(profile => profile.LearningGoal)
            .HasMaxLength(500);

        builder.Property(profile => profile.CurrentEducationLevel)
            .HasMaxLength(100);

        builder.Property(profile => profile.School)
            .HasMaxLength(100);

        builder.Property(profile => profile.Major)
            .HasMaxLength(100);
    }
}
