using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Avancira.Domain.Catalog;

namespace Backend.Infrastructure.Persistence.Configurations.Catalog
{
    public class ListingCategoryConfiguration : IEntityTypeConfiguration<ListingCategory>
    {
        public void Configure(EntityTypeBuilder<ListingCategory> builder)
        {
            builder.HasKey(lc => new { lc.ListingId, lc.CategoryId });

            builder.HasOne(lc => lc.Listing)
                .WithMany(l => l.ListingCategories)
                .HasForeignKey(lc => lc.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(lc => lc.Category)
                .WithMany(c => c.ListingCategories)
                .HasForeignKey(lc => lc.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
