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
        builder.HasIndex(t => new { t.UserId, t.DeviceId }).IsUnique();
        builder.Property(t => t.TokenHash).IsRequired();
        builder.Property(t => t.DeviceId).HasMaxLength(200);
        builder.Property(t => t.IpAddress).HasMaxLength(45);
        builder.Property(t => t.UserAgent).HasMaxLength(512);
        builder.Property(t => t.Latitude);
        builder.Property(t => t.Longitude);
        builder.Property(t => t.CreatedAt);

        builder.HasIndex(t => t.IpAddress);
        builder.HasIndex(t => t.UserAgent);
        builder.HasIndex(t => t.Latitude);
        builder.HasIndex(t => t.Longitude);
        builder.HasIndex(t => t.CreatedAt);
    }
}
