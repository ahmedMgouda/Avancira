using Avancira.Domain.Tutors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Tutors;

public class TutorSubjectConfiguration : IEntityTypeConfiguration<TutorSubject>
{
    public void Configure(EntityTypeBuilder<TutorSubject> builder)
    {
        builder.ToTable("TutorSubjects");

        builder.HasKey(subject => subject.Id);

        builder.Property(subject => subject.HourlyRate)
            .HasPrecision(10, 2);

        builder.Property(subject => subject.AdminComment)
            .HasMaxLength(500);

        builder.Property(subject => subject.CreatedOnUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(subject => new { subject.TutorId, subject.SubjectId })
            .IsUnique();
    }
}
