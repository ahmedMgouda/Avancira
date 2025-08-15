using Hangfire;

namespace Avancira.Infrastructure.Identity.Tokens;

public class TokenCleanupOptions
{
    public int RetentionDays { get; set; } = 0;
    public string Schedule { get; set; } = Cron.Daily();
}

