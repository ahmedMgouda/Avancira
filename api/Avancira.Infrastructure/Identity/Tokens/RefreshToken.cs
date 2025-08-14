using System;

namespace Avancira.Infrastructure.Identity.Tokens;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public string TokenHash { get; set; } = default!;
    public string DeviceId { get; set; } = default!;
    public DateTime Expiry { get; set; }
}
