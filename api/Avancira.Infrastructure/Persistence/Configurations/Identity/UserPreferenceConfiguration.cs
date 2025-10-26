using Avancira.Domain.Users;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Shared.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Avancira.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.ToTable("UserPreferences", IdentityDefaults.SchemaName);

        builder.HasKey(preference => preference.Id);

        builder.Property(preference => preference.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(preference => preference.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(preference => preference.ActiveProfile)
            .HasMaxLength(50)
            .HasDefaultValue("student")
            .IsRequired();

        builder.Property(preference => preference.UpdatedOnUtc)
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(preference => preference.UserId)
            .IsUnique();

        builder.HasOne<User>()
            .WithOne(user => user.UserPreference)
            .HasForeignKey<UserPreference>(preference => preference.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
