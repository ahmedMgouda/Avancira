using Avancira.Application.Identity.Tokens.Dtos;
using System;

namespace Avancira.Application.Identity.Tokens;


public interface ISessionService
{
    Task<List<SessionDto>> GetActiveSessionsAsync(string userId);
    Task RevokeSessionAsync(string userId, Guid sessionId);
}
