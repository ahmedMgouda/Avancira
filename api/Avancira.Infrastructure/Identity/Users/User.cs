using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;
using Microsoft.AspNetCore.Identity;

namespace Avancira.Infrastructure.Identity.Users;
public class User : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Uri? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public string? TimeZoneId { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
    public string? ObjectId { get; set; }
}
