using Avancira.Domain.Subjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Subjects;

public class SubjectCategoryConfiguration : IEntityTypeConfiguration<SubjectCategory>
{
    public void Configure(EntityTypeBuilder<SubjectCategory> builder)
    {
        builder.ToTable("SubjectCategories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(category => category.Description)
            .HasMaxLength(255);

        builder.Property(category => category.IsActive)
            .HasDefaultValue(true);

        builder.Property(category => category.IsVisible)
            .HasDefaultValue(true);

        builder.Property(category => category.IsFeatured)
            .HasDefaultValue(false);

        builder.Property(category => category.SortOrder)
               .IsRequired();

        builder.HasIndex(category => category.SortOrder)
               .IsUnique();

        builder.HasMany(category => category.Subjects)
            .WithOne(subject => subject.Category)
            .HasForeignKey(subject => subject.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
