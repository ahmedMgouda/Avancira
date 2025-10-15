using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Avancira.Domain.UserSessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IdentityConstants = Avancira.Shared.Authorization.IdentityConstants;

namespace Avancira.Infrastructure.Persistence.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("Sessions", IdentityConstants.SchemaName);

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(s => s.DeviceId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(s => s.DeviceName)
            .HasMaxLength(256);

        builder.Property(s => s.UserAgent)
            .HasMaxLength(512);

        builder.Property(s => s.IpAddress)
            .HasMaxLength(64);

        builder.Property(s => s.Status)
            .IsRequired();

        builder.Property(s => s.RefreshTokenReferenceId)
            .HasMaxLength(512);

        builder.Property(s => s.RefreshTokenExpiresAt);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.LastActivityAt)
            .IsRequired();

        builder.Property(s => s.RevokedAt);

        builder.Property(s => s.RevocationReason)
            .HasMaxLength(512);

        builder.Property(s => s.RequiresUserNotification)
            .IsRequired();

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.DeviceId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.RefreshTokenReferenceId);
        builder.HasIndex(s => s.RefreshTokenExpiresAt);
        builder.HasIndex(s => s.LastActivityAt);
        builder.HasIndex(s => s.RevokedAt);
    }
}
