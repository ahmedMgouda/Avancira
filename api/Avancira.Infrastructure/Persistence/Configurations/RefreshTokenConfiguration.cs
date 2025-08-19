using Avancira.Infrastructure.Identity.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IdentityConstants = Avancira.Shared.Authorization.IdentityConstants;

namespace Avancira.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", IdentityConstants.SchemaName);
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).IsRequired();
        builder.Property(t => t.CreatedUtc).IsRequired();
        builder.Property(t => t.RevokedUtc);

        builder.HasIndex(t => t.SessionId);
        builder.HasIndex(t => t.RotatedFromId).IsUnique(false);
        builder.HasIndex(t => t.CreatedUtc);
        builder.HasIndex(t => t.RevokedUtc);

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
