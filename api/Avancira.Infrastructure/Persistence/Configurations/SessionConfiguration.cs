using Avancira.Infrastructure.Identity.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");
        builder.HasKey(s => s.Id);

        builder.HasIndex(s => new { s.UserId, s.Device }).IsUnique();

        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.Device).IsRequired().HasMaxLength(200);
        builder.Property(s => s.UserAgent).HasMaxLength(100);
        builder.Property(s => s.OperatingSystem).HasMaxLength(100);
        builder.Property(s => s.IpAddress).IsRequired().HasMaxLength(45);
        builder.Property(s => s.Country).HasMaxLength(100);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.AbsoluteExpiryUtc).IsRequired();
        builder.Property(s => s.LastRefreshUtc).IsRequired();
        builder.Property(s => s.LastActivityUtc).IsRequired();

        builder.HasIndex(s => s.Device);
        builder.HasIndex(s => s.UserAgent);
        builder.HasIndex(s => s.OperatingSystem);
        builder.HasIndex(s => s.IpAddress);
        builder.HasIndex(s => s.CreatedAt);
        builder.HasIndex(s => s.AbsoluteExpiryUtc);
        builder.HasIndex(s => s.LastRefreshUtc);
        builder.HasIndex(s => s.LastActivityUtc);
    }
}
