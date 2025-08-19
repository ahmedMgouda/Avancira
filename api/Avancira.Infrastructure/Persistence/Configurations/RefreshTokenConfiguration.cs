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

        builder.Property(t => t.TokenHash).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.Revoked).HasDefaultValue(false);
        builder.Property(t => t.RevokedAt);

        builder.HasIndex(t => t.SessionId);
        builder.HasIndex(t => t.RotatedFromId).IsUnique(false);
        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => t.ExpiresAt);
        builder.HasIndex(t => t.Revoked);

        builder.HasOne(t => t.Session)
            .WithMany(s => s.RefreshTokens)
            .HasForeignKey(t => t.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.RotatedFrom)
            .WithOne()
            .HasForeignKey<RefreshToken>(t => t.RotatedFromId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
