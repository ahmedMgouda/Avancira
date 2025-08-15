using Avancira.Infrastructure.Identity.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);

        builder.HasIndex(t => new { t.UserId, t.Device }).IsUnique();

        builder.Property(t => t.TokenHash).IsRequired();
        builder.Property(t => t.Device).IsRequired().HasMaxLength(200);
        builder.Property(t => t.UserAgent).HasMaxLength(100);
        builder.Property(t => t.OperatingSystem).HasMaxLength(100);
        builder.Property(t => t.IpAddress).IsRequired().HasMaxLength(45);
        builder.Property(t => t.Country).HasMaxLength(100);
        builder.Property(t => t.City).HasMaxLength(100);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.Revoked).HasDefaultValue(false);
        builder.Property(t => t.RevokedAt);

        builder.HasIndex(t => t.Device);
        builder.HasIndex(t => t.UserAgent);
        builder.HasIndex(t => t.OperatingSystem);
        builder.HasIndex(t => t.IpAddress);
        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => t.ExpiresAt);
        builder.HasIndex(t => t.Revoked);
    }
}
