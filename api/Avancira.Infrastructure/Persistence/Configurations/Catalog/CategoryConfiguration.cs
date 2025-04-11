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
                .IsRequired();

            builder.HasMany(c => c.ListingCategories)
                .WithOne(lc => lc.Category)
                .HasForeignKey(lc => lc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
