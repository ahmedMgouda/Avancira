using Avancira.Application.UserSessions.Dtos;
using Avancira.Domain.UserSessions;

namespace Avancira.Application.UserSessions;

public interface IUserSessionService
{
    Task<UserSession> CreateAsync(UserSession session, CancellationToken cancellationToken = default);
    Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default);
    Task UpdateActivityAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid sessionId, string? reason = null, CancellationToken cancellationToken = default);
    Task<int> RevokeAllAsync(string userId, Guid? excludeSessionId = null, CancellationToken cancellationToken = default);
    Task RevokeAllUserSessionsAsync(string userId, string reason = "Security event", CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSession>> GetExpiredSessionsAsync(DateTimeOffset beforeDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceSessionsDto>> GetActiveByUserGroupedByDeviceAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSessionDto>> GetActiveByUserAsync(string userId, CancellationToken cancellationToken = default);
}