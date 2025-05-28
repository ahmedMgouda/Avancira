using Microsoft.AspNetCore.Identity;

namespace Avancira.Infrastructure.Identity.Roles;
public class Role : IdentityRole<string>
{
    public string? Description { get; set; }

    public Role(string name, string? description = null)
        : base(name)
    {
        Id = Guid.NewGuid().ToString();
        Description = description;
        NormalizedName = name.ToUpperInvariant();
    }
}