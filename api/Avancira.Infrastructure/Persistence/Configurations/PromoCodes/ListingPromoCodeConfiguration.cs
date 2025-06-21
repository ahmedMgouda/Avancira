using Avancira.Domain.PromoCodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.PromoCodes
{
    public class ListingPromoCodeConfiguration : IEntityTypeConfiguration<ListingPromoCode>
    {
        public void Configure(EntityTypeBuilder<ListingPromoCode> builder)
        {
            builder.HasKey(lpc => new { lpc.ListingId, lpc.PromoCodeId });

            builder.HasOne(lpc => lpc.Listing)
                .WithMany(l => l.ListingPromoCodes)
                .HasForeignKey(lpc => lpc.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(lpc => lpc.PromoCode)
                .WithMany(p => p.ListingPromoCodes)
                .HasForeignKey(lpc => lpc.PromoCodeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
