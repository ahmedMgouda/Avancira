using Avancira.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Catalog
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(c => c.DisplayInLandingPage)
                .IsRequired();

            builder.Property(c => c.ImageUrl)
                .HasConversion(
                    v => v != null ? v.ToString() : null,
                    v => string.IsNullOrEmpty(v) ? null : 
                         (Uri.IsWellFormedUriString(v, UriKind.Absolute) ? new Uri(v, UriKind.Absolute) :
                          Uri.IsWellFormedUriString(v, UriKind.Relative) ? new Uri(v, UriKind.Relative) : null))
                .IsRequired(false);

            builder.HasMany(c => c.ListingCategories)
                .WithOne(lc => lc.Category)
                .HasForeignKey(lc => lc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
