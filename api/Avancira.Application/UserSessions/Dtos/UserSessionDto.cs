using System;
using System.Collections.Generic;
using Avancira.Domain.UserSessions;

namespace Avancira.Application.UserSessions.Dtos;

public class UserSessionDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public string DeviceId { get; set; } = null!;
    public string? DeviceName { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public SessionStatus Status { get; set; }
    public string? RefreshTokenReferenceId { get; set; }
    public DateTimeOffset? TokenExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastActivityAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
    public bool RequiresUserNotification { get; set; }
    public IReadOnlyCollection<string> AccessedResourceIds { get; set; } = Array.Empty<string>();
}
