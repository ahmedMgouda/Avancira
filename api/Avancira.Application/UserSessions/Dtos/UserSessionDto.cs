namespace Avancira.Application.UserSessions.Dtos;

public class UserSessionDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public Guid AuthorizationId { get; set; }
    public string DeviceId { get; set; } = default!;
    public string? DeviceName { get; set; }
    public string? UserAgent { get; set; }
    public string? OperatingSystem { get; set; }
    public string IpAddress { get; set; } = default!;
    public string? Country { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime AbsoluteExpiryUtc { get; set; }
    public DateTime LastActivityUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
