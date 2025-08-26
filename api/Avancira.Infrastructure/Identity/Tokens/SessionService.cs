using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Infrastructure.Persistence;
using System;
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

    public async Task<List<SessionDto>> GetActiveSessionsAsync(string userId)
    {
        var sessions = await _dbContext.Sessions
            .Where(s => s.UserId == userId && s.RevokedUtc == null && s.AbsoluteExpiryUtc > DateTime.UtcNow)
            .ProjectToType<SessionDto>()
            .ToListAsync();

        return sessions;
    }

    public async Task RevokeSessionAsync(string userId, Guid sessionId)
    {
        var session = await _dbContext.Sessions
            .Include(s => s.RefreshTokens)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session is null)
            return;

        var now = DateTime.UtcNow;
        session.RevokedUtc = now;

        foreach (var token in session.RefreshTokens.Where(rt => rt.RevokedUtc == null))
        {
            token.RevokedUtc = now;
        }

        await _dbContext.SaveChangesAsync();
    }
}
