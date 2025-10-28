using Avancira.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Reviews;

public class StudentReviewConfiguration : IEntityTypeConfiguration<StudentReview>
{
    public void Configure(EntityTypeBuilder<StudentReview> builder)
    {
        builder.ToTable("StudentReviews");

        builder.HasKey(review => review.Id);

        builder.Property(review => review.StudentId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(review => review.Comment)
            .HasMaxLength(1000);

        builder.Property(review => review.FlagReason)
            .HasMaxLength(500);

        builder.Property(review => review.TutorResponse)
            .HasMaxLength(1000);

        builder.Property(review => review.ModeratedByAdminId)
            .HasMaxLength(100);

        builder.Property(review => review.CreatedOnUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(review => review.ModeratedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(review => review.TutorRespondedAtUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(review => review.EditedAtUtc)
            .HasColumnType("timestamp without time zone");
    }
}
