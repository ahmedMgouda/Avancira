using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Application.Common;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Persistence;
using Avancira.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Avancira.Infrastructure.Identity.Tokens;

public class SessionService : ISessionService
{
    private readonly AvanciraDbContext _dbContext;

    public SessionService(AvanciraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task StoreSessionAsync(string userId, ClientInfo clientInfo, string refreshToken, DateTime refreshExpiry)
    {
        var existingSession = await _dbContext.Sessions
            .Include(s => s.RefreshTokens)
            .SingleOrDefaultAsync(s => s.UserId == userId && s.Device == clientInfo.DeviceId);
        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        if (existingSession != null)
        {
            _dbContext.RefreshTokens.RemoveRange(existingSession.RefreshTokens);
            _dbContext.Sessions.Remove(existingSession);
        }

        var now = DateTime.UtcNow;
        var session = new Session
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

        session.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = TokenUtilities.HashToken(refreshToken),
            CreatedUtc = now,
            AbsoluteExpiryUtc = refreshExpiry
        });

        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync();
        await tx.CommitAsync();
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
            .ProjectToType<SessionDto>()
            .ToListAsync();

        return sessions;
    }

    public Task RevokeSessionAsync(string userId, Guid sessionId) =>
        RevokeSessionsAsync(userId, new[] { sessionId });

    public async Task RevokeSessionsAsync(string userId, IEnumerable<Guid> sessionIds)
    {
        var sessions = await _dbContext.Sessions
            .Include(s => s.RefreshTokens)
            .Where(s => s.UserId == userId && sessionIds.Contains(s.Id))
            .ToListAsync();

        if (sessions.Count == 0)
            return;

        var now = DateTime.UtcNow;

        foreach (var session in sessions)
        {
            session.RevokedUtc = now;

            foreach (var token in session.RefreshTokens.Where(rt => rt.RevokedUtc == null))
            {
                token.RevokedUtc = now;
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<(string UserId, Guid RefreshTokenId)?> GetRefreshTokenInfoAsync(string tokenHash)
    {
        var token = await _dbContext.RefreshTokens
            .Include(rt => rt.Session)
            .SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.RevokedUtc == null && rt.AbsoluteExpiryUtc > DateTime.UtcNow);

        if (token == null)
            return null;

        return (token.Session.UserId, token.Id);
    }

    public async Task RotateRefreshTokenAsync(Guid refreshTokenId, string newRefreshTokenHash, DateTime newExpiry)
    {
        var token = await _dbContext.RefreshTokens
            .Include(rt => rt.Session)
            .SingleOrDefaultAsync(rt => rt.Id == refreshTokenId);

        if (token == null)
            return;

        var now = DateTime.UtcNow;
        token.RevokedUtc = now;

        var session = token.Session;
        session.LastRefreshUtc = now;
        session.LastActivityUtc = now;
        session.AbsoluteExpiryUtc = newExpiry;

        session.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = newRefreshTokenHash,
            CreatedUtc = now,
            AbsoluteExpiryUtc = newExpiry,
            RotatedFromId = token.Id
        });

        await _dbContext.SaveChangesAsync();
    }
}
