using System;

namespace Avancira.Infrastructure.Identity.Tokens;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string TokenHash { get; set; } = default!;
    public Guid SessionId { get; set; }
    public Guid? RotatedFromId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }

    public Session Session { get; set; } = default!;
    public RefreshToken? RotatedFrom { get; set; }
}
