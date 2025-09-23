using Avancira.Application.UserSessions.Dtos;

namespace Avancira.Application.UserSessions;

/// <summary>
/// Service for managing user sessions (create, query, revoke, update activity).
/// Wraps domain logic with persistence and DTO mapping.
/// </summary>
public interface IUserSessionService
{
    /// <summary>
    /// Gets a user session by its identifier.
    /// </summary>
    Task<UserSessionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active (non-revoked, non-expired) sessions for a given user.
    /// </summary>
    Task<IEnumerable<UserSessionDto>> GetActiveByUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user session with associated metadata and absolute expiry.
    /// </summary>
    Task<UserSessionDto> CreateAsync(CreateUserSessionDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last activity timestamp of a session.
    /// </summary>
    Task<UserSessionDto> UpdateActivityAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific session.
    /// </summary>
    Task<bool> RevokeAsync(Guid sessionId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active sessions for a user (optionally excluding one session).
    /// </summary>
    Task<int> RevokeAllAsync(string userId, Guid? excludeSessionId = null, CancellationToken cancellationToken = default);
}
