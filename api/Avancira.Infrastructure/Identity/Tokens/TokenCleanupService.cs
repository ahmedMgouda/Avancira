using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avancira.Infrastructure.Identity.Tokens;

public class TokenCleanupService
{
    private readonly AvanciraDbContext _dbContext;
    private readonly TokenCleanupOptions _options;
    private readonly ILogger<TokenCleanupService> _logger;

    public TokenCleanupService(
        AvanciraDbContext dbContext,
        IOptions<TokenCleanupOptions> options,
        ILogger<TokenCleanupService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task CleanupAsync()
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddDays(-_options.RetentionDays);

        var tokens = await _dbContext.RefreshTokens
            .Where(t => (t.Revoked && t.RevokedAt <= threshold) || t.ExpiresAt <= threshold)
            .ToListAsync();

        if (tokens.Count == 0)
        {
            return;
        }

        _dbContext.RefreshTokens.RemoveRange(tokens);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Removed {Count} expired or revoked refresh tokens", tokens.Count);
    }
}

