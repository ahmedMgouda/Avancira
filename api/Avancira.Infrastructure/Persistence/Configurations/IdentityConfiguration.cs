using System;
using Avancira.Domain.Auditing;
using Avancira.Domain.Students;
using Avancira.Domain.Tutors;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Identity.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IdentityDefaults = Avancira.Shared.Authorization.IdentityDefaults;

namespace Avancira.Infrastructure.Persistence.Configurations;

public class AuditTrailConfig : IEntityTypeConfiguration<AuditTrail>
{
    public void Configure(EntityTypeBuilder<AuditTrail> builder)
    {
        builder
            .ToTable("AuditTrails", IdentityDefaults.SchemaName);

        builder.HasKey(a => a.Id);
    }
}

public class ApplicationUserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .ToTable("Users", IdentityDefaults.SchemaName);

        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Gender)
            .HasMaxLength(20);

        builder.Property(u => u.TimeZoneId)
            .HasMaxLength(100);

        builder.Property(u => u.ObjectId)
            .HasMaxLength(256);

        builder.Property(u => u.CountryCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.HasOne(u => u.Country)
            .WithMany()
            .HasForeignKey(u => u.CountryCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.PhoneNumberWithoutDialCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.ImageUrl)
            .HasConversion(
                uri => uri != null ? uri.ToString() : null,
                value => string.IsNullOrWhiteSpace(value) ? null : new Uri(value));

        builder.Property(u => u.Bio)
            .HasMaxLength(500);

        builder.Property(u => u.PayPalAccountId)
            .HasMaxLength(255);

        builder.Property(u => u.StripeCustomerId)
            .HasMaxLength(255);

        builder.Property(u => u.StripeConnectedAccountId)
            .HasMaxLength(255);

        builder.Property(u => u.SkypeId)
            .HasMaxLength(255);

        builder.Property(u => u.HangoutId)
            .HasMaxLength(255);

        builder.Property(u => u.CreatedOnUtc)
            .HasColumnType("timestamp without time zone");

        builder.Property(u => u.LastModifiedOnUtc)
            .HasColumnType("timestamp without time zone");

        builder.OwnsOne(u => u.Address, ownedNavigationBuilder =>
        {
            ownedNavigationBuilder.ToTable("UserAddresses", IdentityDefaults.SchemaName);

            ownedNavigationBuilder.Property(address => address.Street)
                .HasMaxLength(255);

            ownedNavigationBuilder.Property(address => address.City)
                .HasMaxLength(100);

            ownedNavigationBuilder.Property(address => address.State)
                .HasMaxLength(100);

            ownedNavigationBuilder.Property(address => address.PostalCode)
                .HasMaxLength(20);
        });

        builder.HasOne(u => u.TutorProfile)
            .WithOne()
            .HasForeignKey<TutorProfile>(profile => profile.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.StudentProfile)
            .WithOne()
            .HasForeignKey<StudentProfile>(profile => profile.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApplicationRoleConfig : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder) =>
        builder
            .ToTable("Roles", IdentityDefaults.SchemaName);
}

public class ApplicationRoleClaimConfig : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder) =>
        builder
            .ToTable("RoleClaims", IdentityDefaults.SchemaName);
}

public class IdentityUserRoleConfig : IEntityTypeConfiguration<IdentityUserRole<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<string>> builder) =>
        builder
            .ToTable("UserRoles", IdentityDefaults.SchemaName);
}

public class IdentityUserClaimConfig : IEntityTypeConfiguration<IdentityUserClaim<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<string>> builder) =>
        builder
            .ToTable("UserClaims", IdentityDefaults.SchemaName);
}

public class IdentityUserLoginConfig : IEntityTypeConfiguration<IdentityUserLogin<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<string>> builder) =>
        builder
            .ToTable("UserLogins", IdentityDefaults.SchemaName);
}

public class IdentityUserTokenConfig : IEntityTypeConfiguration<IdentityUserToken<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<string>> builder) =>
        builder
            .ToTable("UserTokens", IdentityDefaults.SchemaName);
}
