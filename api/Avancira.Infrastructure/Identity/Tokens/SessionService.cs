using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Application.Common;
using Avancira.Infrastructure.Persistence;
using Avancira.Domain.Identity;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public async Task StoreSessionAsync(string userId, Guid sessionId, string refreshTokenId, ClientInfo clientInfo, DateTime refreshExpiry)
    {
        var now = DateTime.UtcNow;
        var session = new Session(sessionId)
        {
            UserId = userId,
            UserAgent = clientInfo.UserAgent,
            OperatingSystem = clientInfo.OperatingSystem,
            IpAddress = clientInfo.IpAddress,
            Country = clientInfo.Country,
            City = clientInfo.City,
            ActiveRefreshTokenId = refreshTokenId,
            CreatedUtc = now,
            LastActivityUtc = now,
            LastRefreshUtc = now,
            AbsoluteExpiryUtc = refreshExpiry
        };

        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> ValidateSessionAsync(string userId, Guid sessionId)
    {
        var session = await _dbContext.Sessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Id == sessionId);

        if (session is null || session.RevokedUtc != null || session.AbsoluteExpiryUtc <= DateTime.UtcNow || string.IsNullOrEmpty(session.ActiveRefreshTokenId))
            return false;

        var token = await _tokenManager.FindByIdAsync(session.ActiveRefreshTokenId);
        if (token == null)
            return false;

        var status = await _tokenManager.GetStatusAsync(token);
        return string.Equals(status, OpenIddictConstants.Statuses.Valid, StringComparison.Ordinal);
    }

    public async Task<List<SessionDto>> GetActiveSessionsAsync(string userId)
    {
        var sessions = await _dbContext.Sessions
            .Where(s => s.UserId == userId && s.RevokedUtc == null && s.AbsoluteExpiryUtc > DateTime.UtcNow)
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

        foreach (var tokenId in sessions.Select(s => s.ActiveRefreshTokenId).Where(id => id != null))
        {
            var token = await _tokenManager.FindByIdAsync(tokenId!);
            if (token != null)
            {
                await _tokenManager.TryRevokeAsync(token);
            }
        }
    }

}
