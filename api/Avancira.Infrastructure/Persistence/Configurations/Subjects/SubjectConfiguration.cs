using Avancira.Domain.Subjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Subjects;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects");

        builder.HasKey(subject => subject.Id);

        builder.Property(subject => subject.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(subject => subject.Description)
            .HasMaxLength(255);

        builder.Property(subject => subject.IconUrl)
            .HasMaxLength(255);

        builder.Property(subject => subject.IsActive)
            .HasDefaultValue(true);

        builder.Property(subject => subject.IsVisible)
            .HasDefaultValue(true);

        builder.Property(subject => subject.IsFeatured)
            .HasDefaultValue(false);

        builder.Property(subject => subject.SortOrder)
            .HasDefaultValue(0);

        builder.Property(subject => subject.CreatedOnUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(subject => subject.UpdatedOnUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(subject => new { subject.CategoryId, subject.SortOrder });
    }
}
