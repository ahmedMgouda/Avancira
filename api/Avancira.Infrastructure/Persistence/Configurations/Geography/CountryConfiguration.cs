using Avancira.Domain.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Geography;

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("Code")
            .HasMaxLength(3)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasMaxLength(3)
            .IsUnicode(false);

        builder.Property(x => x.DialingCode)
            .HasMaxLength(10)
            .IsUnicode(false);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.HasIndex(x => x.Id)
            .IsUnique();
    }
}
