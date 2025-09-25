using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.UserSessions.Events;
using Avancira.Domain.UserSessions.ValueObjects;

namespace Avancira.Domain.UserSessions;

public sealed class UserSession : BaseEntity, IAggregateRoot
{
    private UserSession() { }

    private UserSession(
        string userId,
        Guid authorizationId,
        SessionMetadata metadata,
        DateTime absoluteExpiry)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new AvanciraValidationException("User ID is required");

        if (authorizationId == Guid.Empty)
            throw new AvanciraValidationException("Authorization ID is required");

        UserId = userId;
        AuthorizationId = authorizationId;
        DeviceId = metadata.DeviceId.Value;
        IpAddress = metadata.IpAddress.Value;
        UserAgent = metadata.UserAgent.Value;
        DeviceName = metadata.DeviceInfo.Name;
        OperatingSystem = metadata.DeviceInfo.OperatingSystem;
        Country = metadata.Location?.Country;
        City = metadata.Location?.City;
        CreatedAtUtc = DateTime.UtcNow;
        AbsoluteExpiryUtc = absoluteExpiry;
        LastActivityUtc = CreatedAtUtc;

        QueueDomainEvent(new SessionCreatedEvent(Id, userId, metadata.DeviceInfo.Category));
    }

    // Properties
    public string UserId { get; private set; } = default!;
    public Guid AuthorizationId { get; private set; }
    public string DeviceId { get; private set; } = default!;
    public string? DeviceName { get; private set; }
    public string? UserAgent { get; private set; }
    public string? OperatingSystem { get; private set; }
    public string IpAddress { get; private set; } = default!;
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime AbsoluteExpiryUtc { get; private set; }
    public DateTime LastActivityUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    // Factory method
    public static UserSession Create(
        string userId,
        Guid authorizationId,
        SessionMetadata metadata,
        TimeSpan? duration = null)
    {
        var expiry = DateTime.UtcNow.Add(duration ?? TimeSpan.FromDays(30));
        return new UserSession(userId, authorizationId, metadata, expiry);
    }

    // Business methods
    public void UpdateActivity()
    {
        var now = DateTime.UtcNow;
        if (!IsActiveAt(now))
            throw new AvanciraConflictException("Cannot update activity on inactive session");

        LastActivityUtc = now;
        QueueDomainEvent(new SessionActivityUpdatedEvent(Id, UserId));
    }

    public void Revoke(string? reason = null)
    {
        if (RevokedAtUtc.HasValue)
            throw new AvanciraConflictException("Session is already revoked");

        RevokedAtUtc = DateTime.UtcNow;
        QueueDomainEvent(new SessionRevokedEvent(Id, UserId, reason));
    }

    public void UpdateMetadata(SessionMetadata metadata)
    {
        if (!IsActiveAt(DateTime.UtcNow))
            throw new AvanciraConflictException("Cannot update metadata on inactive session");

        DeviceName = metadata.DeviceInfo.Name;
        UserAgent = metadata.UserAgent.Value;
        OperatingSystem = metadata.DeviceInfo.OperatingSystem;
        Country = metadata.Location?.Country;
        City = metadata.Location?.City;

        QueueDomainEvent(new SessionMetadataUpdatedEvent(Id, UserId));
    }

    // Query methods
    public bool IsActiveAt(DateTime utc) =>
        !RevokedAtUtc.HasValue && AbsoluteExpiryUtc > utc;

    public bool IsExpiredAt(DateTime utc) =>
        AbsoluteExpiryUtc <= utc;

    public TimeSpan TimeUntilExpiry() =>
        AbsoluteExpiryUtc > DateTime.UtcNow
            ? AbsoluteExpiryUtc - DateTime.UtcNow
            : TimeSpan.Zero;
}
