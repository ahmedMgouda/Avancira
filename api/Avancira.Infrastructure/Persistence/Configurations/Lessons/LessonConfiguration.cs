using Avancira.Domain.Lessons;
using Avancira.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Lessons;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lessons");

        builder.HasKey(lesson => lesson.Id);

        builder.Property(lesson => lesson.StudentId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(lesson => lesson.TutorId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(lesson => lesson.ScheduledAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(lesson => lesson.BookedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(lesson => lesson.ConfirmedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(lesson => lesson.StartedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(lesson => lesson.CompletedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(lesson => lesson.CanceledAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(lesson => lesson.MeetingUrl)
            .HasMaxLength(255);

        builder.Property(lesson => lesson.MeetingId)
            .HasMaxLength(100);

        builder.Property(lesson => lesson.MeetingPassword)
            .HasMaxLength(100);

        builder.Property(lesson => lesson.CanceledBy)
            .HasMaxLength(50);

        builder.Property(lesson => lesson.CancellationReason)
            .HasMaxLength(500);

        builder.Property(lesson => lesson.TutorNotes)
            .HasMaxLength(2000);

        builder.Property(lesson => lesson.SessionSummary)
            .HasMaxLength(2000);

        builder.Property(lesson => lesson.FinalPrice)
            .HasPrecision(10, 2);

        builder.HasOne(lesson => lesson.Review)
            .WithOne(review => review.Lesson)
            .HasForeignKey<StudentReview>(review => review.LessonId);
    }
}
