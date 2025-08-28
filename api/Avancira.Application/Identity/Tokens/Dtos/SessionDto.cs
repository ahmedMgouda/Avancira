namespace Avancira.Application.Identity.Tokens.Dtos;

public record SessionDto : IEquatable<SessionDto>
{
    public Guid Id { get; init; }
    public string Device { get; init; }
    public string? UserAgent { get; init; }
    public string? OperatingSystem { get; init; }
    public string IpAddress { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime LastActivityUtc { get; init; }
    public DateTime LastRefreshUtc { get; init; }
    public DateTime AbsoluteExpiryUtc { get; init; }
    public DateTime? RevokedUtc { get; init; }

    // Add a constructor that matches the usage in SessionService
    public SessionDto(
        Guid id,
        string device,
        string? userAgent,
        string? operatingSystem,
        string ipAddress,
        string? country,
        string? city,
        DateTime createdUtc,
        DateTime lastActivityUtc,
        DateTime lastRefreshUtc,
        DateTime absoluteExpiryUtc,
        DateTime? revokedUtc)
    {
        Id = id;
        Device = device;
        UserAgent = userAgent;
        OperatingSystem = operatingSystem;
        IpAddress = ipAddress;
        Country = country;
        City = city;
        CreatedUtc = createdUtc;
        LastActivityUtc = lastActivityUtc;
        LastRefreshUtc = lastRefreshUtc;
        AbsoluteExpiryUtc = absoluteExpiryUtc;
        RevokedUtc = revokedUtc;
    }
}
