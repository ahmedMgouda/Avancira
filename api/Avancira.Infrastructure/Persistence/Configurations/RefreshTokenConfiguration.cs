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
        builder.Property(t => t.Device).HasMaxLength(200);
    }
}
