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
using Microsoft.Extensions.Options;

namespace Avancira.Infrastructure.Identity.Tokens;

public class SessionService : ISessionService
{
    private readonly AvanciraDbContext _dbContext;
    private readonly TokenHashingOptions _options;

    public SessionService(AvanciraDbContext dbContext, IOptions<TokenHashingOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task StoreSessionAsync(string userId, Guid sessionId, ClientInfo clientInfo, string refreshToken, DateTime refreshExpiry)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        while (true)
        {
            var existingSession = await _dbContext.Sessions
                .Include(s => s.RefreshTokens)
                .SingleOrDefaultAsync(s => s.UserId == userId && s.Device == clientInfo.DeviceId);

            if (existingSession == null)
                break;

            _dbContext.RefreshTokens.RemoveRange(existingSession.RefreshTokens);
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

        var salt = TokenUtilities.GenerateSalt();
        session.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = TokenUtilities.HashToken(refreshToken, _options.Secret, salt),
            Salt = salt,
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

    public async Task<(string UserId, Guid RefreshTokenId, Guid SessionId)?> GetRefreshTokenInfoAsync(string refreshToken)
    {
        var candidates = await _dbContext.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.RevokedUtc == null && rt.AbsoluteExpiryUtc > DateTime.UtcNow)
            .Select(rt => new { rt.Id, rt.Session.UserId, rt.SessionId, rt.TokenHash, rt.Salt })
            .ToListAsync();

        foreach (var token in candidates)
        {
            var hash = TokenUtilities.HashToken(refreshToken, _options.Secret, token.Salt);
            if (hash == token.TokenHash)
            {
                return (token.UserId, token.Id, token.SessionId);
            }
        }

        return null;
    }

    public async Task RotateRefreshTokenAsync(Guid refreshTokenId, string newRefreshToken, DateTime newExpiry)
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

        var salt = TokenUtilities.GenerateSalt();
        session.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = TokenUtilities.HashToken(newRefreshToken, _options.Secret, salt),
            Salt = salt,
            CreatedUtc = now,
            AbsoluteExpiryUtc = newExpiry,
            RotatedFromId = token.Id
        });

        await _dbContext.SaveChangesAsync();
    }
}
