using System;
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
    /// <summary>Primary key - correlates with refresh token reference ID at auth server</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to user</summary>
    public string UserId { get; set; } = null!;

    /// <summary>Device identifier (from network context)</summary>
    public string DeviceId { get; set; } = null!;

    /// <summary>Device name/description provided by client</summary>
    public string? DeviceName { get; set; }

    /// <summary>User agent from the login request</summary>
    public string? UserAgent { get; set; }

    /// <summary>IP address of the login</summary>
    public string? IpAddress { get; set; }

    /// <summary>Status of the session</summary>
    public SessionStatus Status { get; set; } = SessionStatus.Active;

    /// <summary>Reference ID of the refresh token at auth server (OpenIddict RefreshTokenId)</summary>
    public string? RefreshTokenReferenceId { get; set; }

    /// <summary>When the refresh token expires</summary>
    public DateTimeOffset? TokenExpiresAt { get; set; }

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

    /// <summary>For future use: multi-device logout support</summary>
    public ICollection<string> AccessedResourceIds { get; set; } = new List<string>();

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
