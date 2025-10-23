using Avancira.Domain.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Geography;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries");

        builder.HasKey(country => country.Id);

        builder.Property(country => country.Id)
            .HasColumnName("Code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(country => country.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(country => country.CurrencyCode)
            .HasMaxLength(3);

        builder.Property(country => country.DialingCode)
            .HasMaxLength(5);

        builder.Property(country => country.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(country => country.Name)
            .IsUnique();
    }
}
