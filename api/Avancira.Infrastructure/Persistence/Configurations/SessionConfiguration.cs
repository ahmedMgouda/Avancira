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
        builder.HasIndex(s => s.Id)
            .HasDatabaseName("IX_UserSessions_SessionId")
            .IsUnique();
        builder.Property(s => s.UserAgent).HasMaxLength(100);
        builder.Property(s => s.OperatingSystem).HasMaxLength(100);
        builder.Property(s => s.IpAddress).HasMaxLength(45);
        builder.Property(s => s.Country).HasMaxLength(100);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.ActiveRefreshTokenId).HasMaxLength(100);
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.ActiveRefreshTokenId);
        builder.HasIndex(s => s.UserAgent);
        builder.HasIndex(s => s.OperatingSystem);
        builder.HasIndex(s => s.IpAddress);
        builder.HasIndex(s => s.CreatedUtc);
        builder.HasIndex(s => s.AbsoluteExpiryUtc);
        builder.HasIndex(s => s.LastRefreshUtc);
        builder.HasIndex(s => s.LastActivityUtc);
        builder.HasIndex(s => s.RevokedUtc);
    }
}
