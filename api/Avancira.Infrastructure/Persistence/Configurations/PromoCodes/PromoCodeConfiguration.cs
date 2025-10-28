using Avancira.Domain.PromoCodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.PromoCodes
{
    public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
    {
        public void Configure(EntityTypeBuilder<PromoCode> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Code)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.DiscountAmount)
                .HasPrecision(18, 2)
                .IsRequired(false);

            builder.Property(p => p.DiscountPercentage)
                .HasPrecision(5, 2)
                .IsRequired(false);

            builder.Property(p => p.MaxUsageCount)
                .IsRequired();

            builder.Property(p => p.UsageCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(p => p.StartDate)
                .IsRequired();

            builder.Property(p => p.EndDate)
                .IsRequired();

            builder.Property(p => p.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(p => p.Type)
                .IsRequired();

            builder.HasIndex(p => p.Code);
        }
    }
}
