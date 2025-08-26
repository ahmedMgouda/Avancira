using Avancira.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IdentityConstants = Avancira.Shared.Authorization.IdentityConstants;

namespace Avancira.Infrastructure.Persistence.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions", IdentityConstants.SchemaName);
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Device).HasMaxLength(200);
        builder.Property(s => s.UserAgent).HasMaxLength(100);
        builder.Property(s => s.OperatingSystem).HasMaxLength(100);
        builder.Property(s => s.IpAddress).HasMaxLength(45);
        builder.Property(s => s.Country).HasMaxLength(100);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.HasIndex(s => s.Device);
        builder.HasIndex(s => s.UserAgent);
        builder.HasIndex(s => s.OperatingSystem);
        builder.HasIndex(s => s.IpAddress);
        builder.HasIndex(s => s.CreatedUtc);
        builder.HasIndex(s => s.AbsoluteExpiryUtc);
        builder.HasIndex(s => s.LastRefreshUtc);
        builder.HasIndex(s => s.LastActivityUtc);
        builder.HasIndex(s => s.RevokedUtc);
        builder.HasIndex(s => new { s.UserId, s.Device }).IsUnique();
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", IdentityConstants.SchemaName);
        builder.HasKey(r => r.Id);
        builder.HasOne(r => r.Session)
            .WithMany(s => s.RefreshTokens)
            .HasForeignKey(r => r.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.RotatedFrom)
            .WithMany(r => r.RefreshTokens)
            .HasForeignKey(r => r.RotatedFromId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.SessionId);
        builder.HasIndex(r => r.RotatedFromId);
        builder.HasIndex(r => r.CreatedUtc);
        builder.HasIndex(r => r.AbsoluteExpiryUtc);
        builder.HasIndex(r => r.RevokedUtc);
    }
}
