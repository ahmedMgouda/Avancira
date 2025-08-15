using System;

namespace Avancira.Infrastructure.Identity.Tokens;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public string TokenHash { get; set; } = default!;
    public string DeviceId { get; set; } = default!;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime Expiry { get; set; }
}
