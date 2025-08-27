namespace Avancira.Domain.Identity;

using Avancira.Domain.Common;

public class RefreshToken : BaseEntity<Guid>
{
    public string TokenHash { get; set; } = default!;
    public string Salt { get; set; } = default!;
    public Guid SessionId { get; set; }
    public Guid? RotatedFromId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime AbsoluteExpiryUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
    public Session Session { get; set; } = default!;
    public RefreshToken? RotatedFrom { get; set; }
    public ICollection<RefreshToken>? RefreshTokens { get; set; }
}
