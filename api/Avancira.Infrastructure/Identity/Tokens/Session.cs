using System;
using System.Collections.Generic;

namespace Avancira.Infrastructure.Identity.Tokens;

public class Session
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public string Device { get; set; } = default!;
    public string? UserAgent { get; set; }
    public string? OperatingSystem { get; set; }
    public string IpAddress { get; set; } = default!;
    public string? Country { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime AbsoluteExpiryUtc { get; set; }
    public DateTime LastRefreshUtc { get; set; }
    public DateTime LastActivityUtc { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
