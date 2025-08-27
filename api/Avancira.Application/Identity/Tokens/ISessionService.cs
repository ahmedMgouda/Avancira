using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Application.Common;
using System;
using System.Collections.Generic;

namespace Avancira.Application.Identity.Tokens;


public interface ISessionService
{
    Task<List<SessionDto>> GetActiveSessionsAsync(string userId);
    Task RevokeSessionAsync(string userId, Guid sessionId);
    Task RevokeSessionsAsync(string userId, IEnumerable<Guid> sessionIds);
    Task<bool> ValidateSessionAsync(string userId, Guid sessionId);
    Task<(string UserId, Guid RefreshTokenId, Guid SessionId)?> GetRefreshTokenInfoAsync(string tokenHash);
    Task RotateRefreshTokenAsync(Guid refreshTokenId, string newRefreshTokenHash, DateTime newExpiry);
    Task StoreSessionAsync(string userId, Guid sessionId, ClientInfo clientInfo, string refreshToken, DateTime refreshExpiry);
}
