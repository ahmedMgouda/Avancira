using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Avancira.Infrastructure.Identity.Users;
using IdentityDefaults = Avancira.Shared.Authorization.IdentityDefaults;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Domain.Auditing;

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

        builder
            .Property(u => u.ObjectId)
                .HasMaxLength(256);
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