using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Common;

public abstract class BaseSeeder
{
    public abstract Task SeedAsync(CancellationToken cancellationToken = default);
}

public abstract class BaseSeeder<TSeeder> : BaseSeeder
{
    protected readonly ILogger<TSeeder> Logger;

    protected BaseSeeder(ILogger<TSeeder> logger)
        => Logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
