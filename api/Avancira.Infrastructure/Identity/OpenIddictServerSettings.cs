using System;

namespace Avancira.Infrastructure.Identity;

public class OpenIddictServerSettings
{
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromMinutes(3);

    public TimeSpan AuthorizationCodeLifetime { get; set; } = TimeSpan.FromMinutes(5);
}
