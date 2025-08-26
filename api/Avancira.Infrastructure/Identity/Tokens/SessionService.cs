using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Infrastructure.Persistence;
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
}
