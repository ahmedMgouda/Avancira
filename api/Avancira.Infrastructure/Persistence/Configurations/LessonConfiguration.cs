using Backend.Domain.Lessons;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Avancira.Infrastructure.Persistence.Configurations;
public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Date)
            .IsRequired();

        builder.Property(l => l.Duration)
            .IsRequired();

        builder.Property(l => l.HourlyRate)
             .HasPrecision(18, 2)
             .IsRequired();

        builder.Property(l => l.OfferedPrice)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(l => l.StudentId)
            .IsRequired();

        builder.Property(l => l.ListingId)
            .IsRequired();

        builder.Property(l => l.TransactionId)
            .IsRequired();

        builder.Property(l => l.IsStudentInitiated)
            .IsRequired();

        builder.Property(l => l.Status)
            .IsRequired()
            .HasDefaultValue(LessonStatus.Proposed);

        builder.Property(l => l.MeetingToken)
            .HasMaxLength(255);

        builder.Property(l => l.MeetingRoomName)
            .HasMaxLength(255);

        builder.Property(l => l.MeetingUrl)
            .HasMaxLength(255);

        builder.Property(l => l.PromoCodeValue)
            .HasMaxLength(50);

        builder.Property(l => l.PromoDiscount)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.HasOne(l => l.PromoCode)
            .WithMany()
            .HasForeignKey(l => l.PromoCodeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.Listing)
            .WithMany()
            .HasForeignKey(l => l.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Transaction)
            .WithMany()
            .HasForeignKey(l => l.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => l.StudentId);
        builder.HasIndex(l => l.PromoCodeId);
    }
}