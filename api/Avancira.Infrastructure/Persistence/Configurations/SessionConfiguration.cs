using Avancira.Domain.UserSessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IdentityConstants = Avancira.Shared.Authorization.IdentityConstants;

namespace Avancira.Infrastructure.Persistence.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("Sessions", IdentityConstants.SchemaName);

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.Id)
            .HasDatabaseName("IX_UserSessions_SessionId")
            .IsUnique();

        builder.Property(s => s.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.AuthorizationId)
            .IsRequired();

        builder.Property(s => s.DeviceId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.DeviceName)
            .HasMaxLength(200);

        builder.Property(s => s.UserAgent)
            .HasMaxLength(500);

        builder.Property(s => s.OperatingSystem)
            .HasMaxLength(200);

        builder.Property(s => s.IpAddress)
            .IsRequired()
            .HasMaxLength(45); // IPv6 max length

        builder.Property(s => s.Country)
            .HasMaxLength(100);

        builder.Property(s => s.City)
            .HasMaxLength(100);

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        builder.Property(s => s.AbsoluteExpiryUtc)
            .IsRequired();

        builder.Property(s => s.LastActivityUtc)
            .IsRequired();

        builder.Property(s => s.RevokedAtUtc);

        // Useful indexes
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.AuthorizationId);
        builder.HasIndex(s => s.DeviceId);
        builder.HasIndex(s => s.IpAddress);
        builder.HasIndex(s => s.CreatedAtUtc);
        builder.HasIndex(s => s.AbsoluteExpiryUtc);
        builder.HasIndex(s => s.LastActivityUtc);
        builder.HasIndex(s => s.RevokedAtUtc);
    }
}
