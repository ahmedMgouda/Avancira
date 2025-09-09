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
    Task<bool> ValidateSessionAsync(string userId, Guid sessionId, string? refreshTokenHash = null);
    Task StoreSessionAsync(string userId, Guid sessionId, string refreshTokenHash, ClientInfo clientInfo, DateTime refreshExpiry);
    Task UpdateSessionAsync(string userId, Guid sessionId, string refreshTokenHash, DateTime newExpiry);
}
