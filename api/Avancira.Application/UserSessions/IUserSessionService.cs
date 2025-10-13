using Avancira.Domain.UserSessions;

namespace Avancira.Application.UserSessions;

/// <summary>
/// Defines persistence and business operations for user sessions.
/// Provides methods to create, update, revoke, and query user sessions.
/// </summary>
public interface IUserSessionService
{
    Task<UserSession> CreateAsync(UserSession session, CancellationToken cancellationToken = default);
    Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid sessionId, string? reason = null, CancellationToken cancellationToken = default);
    Task<int> RevokeAllAsync(string userId, Guid? excludeSessionId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSession>> GetExpiredSessionsAsync(DateTimeOffset beforeDate, CancellationToken cancellationToken = default);
}
