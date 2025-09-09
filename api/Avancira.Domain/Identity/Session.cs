namespace Avancira.Domain.Identity;

using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;

public class Session : BaseEntity<Guid>, IAggregateRoot
{
    public Session()
    {
    }

    public Session(Guid id) : this()
    {
        Id = id;
    }

    public string UserId { get; set; } = default!;
    public string? UserAgent { get; set; }
    public string? OperatingSystem { get; set; }
    public string IpAddress { get; set; } = default!;
    public string? Country { get; set; }
    public string? City { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime AbsoluteExpiryUtc { get; set; }
    public DateTime LastRefreshUtc { get; set; }
    public DateTime LastActivityUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
}
