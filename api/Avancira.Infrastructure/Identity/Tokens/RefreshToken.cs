using System;

namespace Avancira.Infrastructure.Identity.Tokens;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public string TokenHash { get; set; } = default!;
    public string Device { get; set; } = default!;
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string IpAddress { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; }
    public DateTime? RevokedAt { get; set; }
}
