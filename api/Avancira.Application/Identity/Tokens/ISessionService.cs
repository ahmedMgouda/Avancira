using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Application.Common;
using System;
using System.Collections.Generic;
using Avancira.Domain.Identity;

namespace Avancira.Application.Identity.Tokens;


public interface ISessionService
{
    Task<List<SessionDto>> GetActiveSessionsAsync(string userId);
    Task<UserSession?> GetByRefreshTokenIdAsync(string refreshTokenId);
    Task RevokeSessionAsync(string userId, Guid sessionId);
    Task RevokeSessionsAsync(string userId, IEnumerable<Guid> sessionIds);
    Task<bool> ValidateSessionAsync(string userId, Guid sessionId);
    Task StoreSessionAsync(string userId, Guid sessionId, string refreshTokenId, ClientInfo clientInfo, DateTime refreshExpiry);
    Task UpdateSessionActivityAsync(Guid sessionId, string refreshTokenId, DateTime refreshExpiry);
    Task UpdateLastActivityAsync(Guid sessionId);
}
