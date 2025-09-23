using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Application.Common;
using Avancira.Infrastructure.Persistence;
using Avancira.Domain.Identity;
using OpenIddict.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Identity.Tokens;

public class SessionService : ISessionService
{
    private readonly AvanciraDbContext _dbContext;
    private readonly IOpenIddictTokenManager _tokenManager;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        AvanciraDbContext dbContext,
        IOpenIddictTokenManager tokenManager,
        ILogger<SessionService> logger)
    {
        _dbContext = dbContext;
        _tokenManager = tokenManager;
        _logger = logger;
    }

    public async Task StoreSessionAsync(string userId, Guid sessionId, string refreshTokenId, ClientInfo clientInfo, DateTime refreshExpiry)
    {
        try
        {
            // Create new session
            var now = DateTime.UtcNow;
            var session = new Session(sessionId)
            {
                UserId = userId,
                UserAgent = SanitizeInput(clientInfo.UserAgent),
                OperatingSystem = SanitizeInput(clientInfo.OperatingSystem),
                IpAddress = SanitizeInput(clientInfo.IpAddress),
                Country = SanitizeInput(clientInfo.Country),
                City = SanitizeInput(clientInfo.City),
                ActiveRefreshTokenId = refreshTokenId,
                CreatedUtc = now,
                LastActivityUtc = now,
                LastRefreshUtc = now,
                AbsoluteExpiryUtc = refreshExpiry
            };

            _dbContext.Sessions.Add(session);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("New session {SessionId} created for user {UserId}", sessionId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store session {SessionId} for user {UserId}", sessionId, userId);
            throw;
        }
    }

    public async Task<UserSession?> GetByRefreshTokenIdAsync(string refreshTokenId)
    {
        return await _dbContext.Sessions
            .FirstOrDefaultAsync(s => s.ActiveRefreshTokenId == refreshTokenId && s.RevokedUtc == null);
    }

    private async Task UpdateExistingSessionAsync(UserSession session, string refreshTokenId, DateTime refreshExpiry)
    {
        var now = DateTime.UtcNow;

        // Revoke the old refresh token if it exists and is different
        if (!string.IsNullOrEmpty(session.ActiveRefreshTokenId) &&
            session.ActiveRefreshTokenId != refreshTokenId)
        {
            await RevokeTokenSafelyAsync(session.ActiveRefreshTokenId);
        }

        session.ActiveRefreshTokenId = refreshTokenId;
        session.LastRefreshUtc = now;
        session.LastActivityUtc = now;
        session.AbsoluteExpiryUtc = refreshExpiry;

        await _dbContext.SaveChangesAsync();

        _logger.LogDebug("Updated existing session {SessionId}", session.Id);
    }

    public async Task UpdateSessionActivityAsync(Guid sessionId, string refreshTokenId, DateTime refreshExpiry)
    {
        try
        {
            var rowsAffected = await _dbContext.Sessions
                .Where(s => s.Id == sessionId && s.RevokedUtc == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.ActiveRefreshTokenId, refreshTokenId)
                    .SetProperty(x => x.LastRefreshUtc, DateTime.UtcNow)
                    .SetProperty(x => x.LastActivityUtc, DateTime.UtcNow)
                    .SetProperty(x => x.AbsoluteExpiryUtc, refreshExpiry));

            if (rowsAffected == 0)
            {
                _logger.LogWarning("No session found to update for sessionId {SessionId}", sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update session activity for {SessionId}", sessionId);
            throw;
        }
    }

    public async Task UpdateLastActivityAsync(Guid sessionId)
    {
        try
        {
            var rowsAffected = await _dbContext.Sessions
                .Where(s => s.Id == sessionId && s.RevokedUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastActivityUtc, DateTime.UtcNow));

            if (rowsAffected == 0)
            {
                _logger.LogWarning("No active session found to update activity for {SessionId}", sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update last activity for session {SessionId}", sessionId);
            // Don't rethrow - this is not critical
        }
    }

    public async Task<bool> ValidateSessionAsync(string userId, Guid sessionId)
    {
        try
        {
            var session = await _dbContext.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Id == sessionId);

            if (session is null)
            {
                _logger.LogDebug("Session {SessionId} not found for user {UserId}", sessionId, userId);
                return false;
            }

            if (session.RevokedUtc != null)
            {
                _logger.LogDebug("Session {SessionId} was revoked at {RevokedTime}", sessionId, session.RevokedUtc);
                return false;
            }

            if (session.AbsoluteExpiryUtc <= DateTime.UtcNow)
            {
                _logger.LogDebug("Session {SessionId} expired at {ExpiryTime}", sessionId, session.AbsoluteExpiryUtc);
                return false;
            }

            if (string.IsNullOrEmpty(session.ActiveRefreshTokenId))
            {
                _logger.LogDebug("Session {SessionId} has no active refresh token", sessionId);
                return false;
            }

            // Validate the associated refresh token
            var token = await _tokenManager.FindByIdAsync(session.ActiveRefreshTokenId);
            if (token == null)
            {
                _logger.LogWarning("Refresh token {TokenId} not found for session {SessionId}",
                    session.ActiveRefreshTokenId, sessionId);
                return false;
            }

            var status = await _tokenManager.GetStatusAsync(token);
            var isValid = string.Equals(status, OpenIddictConstants.Statuses.Valid, StringComparison.Ordinal);

            if (!isValid)
            {
                _logger.LogDebug("Refresh token {TokenId} has invalid status: {Status}",
                    session.ActiveRefreshTokenId, status);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session {SessionId} for user {UserId}", sessionId, userId);
            return false;
        }
    }

    public async Task<List<SessionDto>> GetActiveSessionsAsync(string userId)
    {
        try
        {
            var sessions = await _dbContext.Sessions
                .AsNoTracking()
                .Where(s => s.UserId == userId &&
                           s.RevokedUtc == null &&
                           s.AbsoluteExpiryUtc > DateTime.UtcNow)
                .OrderByDescending(s => s.LastActivityUtc)
                .Select(s => new SessionDto(
                    s.Id,
                    s.UserAgent,
                    s.OperatingSystem,
                    s.IpAddress,
                    s.Country,
                    s.City,
                    s.CreatedUtc,
                    s.LastActivityUtc,
                    s.LastRefreshUtc,
                    s.AbsoluteExpiryUtc,
                    s.RevokedUtc))
                .ToListAsync();

            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active sessions for user {UserId}", userId);
            throw;
        }
    }

    public Task RevokeSessionAsync(string userId, Guid sessionId) =>
        RevokeSessionsAsync(userId, new[] { sessionId });

    public async Task RevokeSessionsAsync(string userId, IEnumerable<Guid> sessionIds)
    {
        var ids = sessionIds.ToList();
        if (ids.Count == 0) return;

        try
        {
            // Get sessions with their refresh token IDs in a single query
            var sessionsToRevoke = await _dbContext.Sessions
                .Where(s => s.UserId == userId && ids.Contains(s.Id) && s.RevokedUtc == null)
                .Select(s => new { s.Id, s.ActiveRefreshTokenId })
                .ToListAsync();

            if (sessionsToRevoke.Count == 0)
            {
                _logger.LogWarning("No active sessions found to revoke for user {UserId}", userId);
                return;
            }

            // Mark sessions as revoked using bulk update
            var now = DateTime.UtcNow;
            await _dbContext.Sessions
                .Where(s => s.UserId == userId && ids.Contains(s.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedUtc, now));

            // Revoke associated refresh tokens
            var tokenRevocationTasks = sessionsToRevoke
                .Where(s => !string.IsNullOrEmpty(s.ActiveRefreshTokenId))
                .Select(s => RevokeTokenSafelyAsync(s.ActiveRefreshTokenId!));

            await Task.WhenAll(tokenRevocationTasks);

            _logger.LogInformation("Revoked {Count} sessions for user {UserId}",
                sessionsToRevoke.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke sessions for user {UserId}", userId);
            throw;
        }
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        try
        {
            var cutoffDate = DateTime.UtcNow;
            var expiredSessions = await _dbContext.Sessions
                .Where(s => s.AbsoluteExpiryUtc <= cutoffDate && s.RevokedUtc == null)
                .Select(s => new { s.Id, s.ActiveRefreshTokenId })
                .ToListAsync();

            if (expiredSessions.Count == 0) return;

            // Mark as revoked
            var expiredIds = expiredSessions.Select(s => s.Id).ToList();
            await _dbContext.Sessions
                .Where(s => expiredIds.Contains(s.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedUtc, DateTime.UtcNow));

            // Revoke tokens
            var tokenRevocationTasks = expiredSessions
                .Where(s => !string.IsNullOrEmpty(s.ActiveRefreshTokenId))
                .Select(s => RevokeTokenSafelyAsync(s.ActiveRefreshTokenId!));

            await Task.WhenAll(tokenRevocationTasks);

            _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired sessions");
        }
    }

    private async Task RevokeTokenSafelyAsync(string tokenId)
    {
        try
        {
            var token = await _tokenManager.FindByIdAsync(tokenId);
            if (token != null)
            {
                await _tokenManager.TryRevokeAsync(token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to revoke token {TokenId}, continuing...", tokenId);
        }
    }

    private static string? SanitizeInput(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Truncate to reasonable length and trim
        return input.Length > 500 ? input[..500].Trim() : input.Trim();
    }
}