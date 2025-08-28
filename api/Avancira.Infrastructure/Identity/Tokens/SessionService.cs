using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Application.Common;
using Avancira.Infrastructure.Persistence;
using Avancira.Domain.Identity;
using Avancira.Infrastructure.Auth;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Avancira.Infrastructure.Identity.Tokens;

public class SessionService : ISessionService
{
    private readonly AvanciraDbContext _dbContext;
    private readonly IOpenIddictTokenManager _tokenManager;

    public SessionService(AvanciraDbContext dbContext, IOpenIddictTokenManager tokenManager)
    {
        _dbContext = dbContext;
        _tokenManager = tokenManager;
    }

    public async Task StoreSessionAsync(string userId, Guid sessionId, ClientInfo clientInfo, DateTime refreshExpiry)
    {
        var existingSession = await _dbContext.Sessions
            .SingleOrDefaultAsync(s => s.UserId == userId && s.Device == clientInfo.DeviceId);

        if (existingSession != null)
        {
            _dbContext.Sessions.Remove(existingSession);
            await _dbContext.SaveChangesAsync();
        }

        var now = DateTime.UtcNow;
        var session = new Session(sessionId)
        {
            UserId = userId,
            Device = clientInfo.DeviceId,
            UserAgent = clientInfo.UserAgent,
            OperatingSystem = clientInfo.OperatingSystem,
            IpAddress = clientInfo.IpAddress,
            Country = clientInfo.Country,
            City = clientInfo.City,
            CreatedUtc = now,
            LastActivityUtc = now,
            LastRefreshUtc = now,
            AbsoluteExpiryUtc = refreshExpiry
        };

        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync();
    }

    public Task<bool> ValidateSessionAsync(string userId, Guid sessionId) =>
        _dbContext.Sessions.AnyAsync(s =>
            s.UserId == userId &&
            s.Id == sessionId &&
            s.RevokedUtc == null &&
            s.AbsoluteExpiryUtc > DateTime.UtcNow);

    public async Task<List<SessionDto>> GetActiveSessionsAsync(string userId)
    {
        var sessions = await _dbContext.Sessions
            .Where(s => s.UserId == userId && s.RevokedUtc == null && s.AbsoluteExpiryUtc > DateTime.UtcNow)
            .Select(s => new SessionDto(
                s.Id,
                s.Device,
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

    public Task RevokeSessionAsync(string userId, Guid sessionId) =>
        RevokeSessionsAsync(userId, new[] { sessionId });

    public async Task RevokeSessionsAsync(string userId, IEnumerable<Guid> sessionIds)
    {
        var ids = sessionIds.ToList();

        var sessions = await _dbContext.Sessions
            .Where(s => s.UserId == userId && ids.Contains(s.Id))
            .ToListAsync();

        if (sessions.Count == 0)
            return;

        var now = DateTime.UtcNow;

        foreach (var session in sessions)
        {
            session.RevokedUtc = now;
        }

        await _dbContext.SaveChangesAsync();

        await foreach (var token in _tokenManager.FindBySubjectAsync(userId))
        {
            var type = await _tokenManager.GetTypeAsync(token);
            if (!string.Equals(type, OpenIddictConstants.TokenTypeHints.RefreshToken, StringComparison.Ordinal))
                continue;

            var claims = await _tokenManager.GetClaimsAsync(token);
            var sid = claims.FirstOrDefault(c => c.Type == AuthConstants.Claims.SessionId)?.Value;
            if (sid is null || !Guid.TryParse(sid, out var sidGuid))
                continue;

            if (ids.Contains(sidGuid))
            {
                await _tokenManager.TryRevokeAsync(token);
            }
        }
    }

    public async Task UpdateSessionAsync(string userId, Guid sessionId, DateTime newExpiry)
    {
        var session = await _dbContext.Sessions
            .SingleOrDefaultAsync(s => s.UserId == userId && s.Id == sessionId);

        if (session == null)
            return;

        var now = DateTime.UtcNow;
        session.LastRefreshUtc = now;
        session.LastActivityUtc = now;
        session.AbsoluteExpiryUtc = newExpiry;

        await _dbContext.SaveChangesAsync();
    }
}
