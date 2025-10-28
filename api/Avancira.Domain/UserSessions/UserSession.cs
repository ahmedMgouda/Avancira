using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using Avancira.Domain.Common.Contracts;
using Avancira.Domain.Common.Events;

namespace Avancira.Domain.UserSessions;

/// <summary>
/// Represents a user session tracked across devices
/// Stored in database for audit trail and multi-device tracking
/// </summary>
public class UserSession : IAggregateRoot
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = null!;

    public string DeviceId { get; set; } = null!;

    public string? DeviceName { get; set; }

    public string? UserAgent { get; set; }

    public string? IpAddress { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Active;

    /// <summary>Reference ID of the refresh token at auth server (OpenIddict RefreshTokenId)</summary>
    public string? RefreshTokenReferenceId { get; set; }

    /// <summary>When the refresh token expires</summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    /// <summary>When this session was created</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Last activity on this session</summary>
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When session was revoked/logged out</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Reason for revocation (manual logout, expired, security event, etc)</summary>
    public string? RevocationReason { get; set; }

    /// <summary>True if we should notify user (security event)</summary>
    public bool RequiresUserNotification { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public Collection<DomainEvent> DomainEvents { get; } = new();
}

/// <summary>Session status enum</summary>
public enum SessionStatus
{
    /// <summary>Active and valid</summary>
    Active = 0,

    /// <summary>Refresh token expired but not yet cleaned up</summary>
    Expired = 1,

    /// <summary>Manually revoked by user or admin</summary>
    Revoked = 2,

    /// <summary>Revoked due to security incident</summary>
    RevokedBySecurityEvent = 3,

    /// <summary>Revoked because refresh token was invalidated</summary>
    RevokedByTokenInvalidation = 4
}
