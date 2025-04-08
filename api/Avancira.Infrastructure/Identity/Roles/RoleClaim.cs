using Microsoft.AspNetCore.Identity;

namespace Avancira.Infrastructure.Identity.Roles;
public class RoleClaim : IdentityRoleClaim<string>
{
    public string? CreatedBy { get; init; }
    public DateTimeOffset CreatedOn { get; init; }
}

