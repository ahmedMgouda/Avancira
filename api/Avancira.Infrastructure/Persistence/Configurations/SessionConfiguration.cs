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

        builder.Property(s => s.TokenExpiresAt);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.LastActivityAt)
            .IsRequired();

        builder.Property(s => s.RevokedAt);

        builder.Property(s => s.RevocationReason)
            .HasMaxLength(512);

        builder.Property(s => s.RequiresUserNotification)
            .IsRequired();

        var comparer = new ValueComparer<ICollection<string>>(
            (left, right) =>
                left != null && right != null && left.SequenceEqual(right),
            collection =>
                collection == null
                    ? 0
                    : collection.Aggregate(0, (current, value) => HashCode.Combine(current, value.GetHashCode())),
            collection =>
                collection == null
                    ? new List<string>()
                    : collection.ToList());

        builder.Property(s => s.AccessedResourceIds)
            .HasConversion(
                collection => JsonSerializer.Serialize(collection, (JsonSerializerOptions?)null),
                json => string.IsNullOrWhiteSpace(json)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>())
            .Metadata.SetValueComparer(comparer);

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.DeviceId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.RefreshTokenReferenceId);
        builder.HasIndex(s => s.TokenExpiresAt);
        builder.HasIndex(s => s.LastActivityAt);
        builder.HasIndex(s => s.RevokedAt);
    }
}
