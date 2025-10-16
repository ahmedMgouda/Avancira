using Avancira.Infrastructure.Common;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;

namespace Avancira.Infrastructure.Identity.Seeders;

/// <summary>
/// Seeds OpenId application clients (BFF, Postman, etc.)
/// </summary>
internal sealed class OpenIddictClientSeeder(
    ILogger<OpenIddictClientSeeder> logger,
    IOpenIddictApplicationManager? manager = null
) : BaseSeeder<OpenIddictClientSeeder>(logger)
{
    private readonly IOpenIddictApplicationManager? _manager = manager;

    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (_manager is null)
        {
            Logger.LogWarning(
                "{Seeder} requires {Manager} which is not registered. Skipping OpenIddict client seeding.",
                nameof(OpenIddictClientSeeder),
                nameof(IOpenIddictApplicationManager));
            return;
        }

        Logger.LogInformation("Starting OpenIddict application seeding...");

        await SeedBffClientAsync(_manager, cancellationToken);
        await SeedPostmanClientAsync(_manager, cancellationToken);

        Logger.LogInformation("OpenIddict application seeding completed");
    }

    // ============================================================
    // BFF CLIENT (Web, Authorization Code + PKCE)
    // ============================================================
    private async Task SeedBffClientAsync(IOpenIddictApplicationManager manager, CancellationToken ct)
    {
        const string clientId = "bff-client";
        const string clientSecret = "dev-bff-secret";

        if (await manager.FindByClientIdAsync(clientId, ct) is not null)
        {
            Logger.LogInformation("BFF client already exists, skipping.");
            return;
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = "Avancira BFF (Web Application)",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Explicit,

            RedirectUris =
            {
                new Uri("https://localhost:9200/bff/signin-oidc")
            },
            PostLogoutRedirectUris =
            {
                new Uri("https://localhost:9200/bff/signout-callback-oidc")
            },
            Permissions =
            {
                // Endpoints
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Revocation,

                // Grant types
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,

                // Response types
                OpenIddictConstants.Permissions.ResponseTypes.Code,

                // Scopes
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Profile,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Email,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess,
                OpenIddictConstants.Permissions.Prefixes.Scope + "api"
            },
            Requirements =
            {
                // PKCE required
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };

        await manager.CreateAsync(descriptor, ct);
        Logger.LogInformation("Seeded BFF client: {ClientId}", clientId);
    }

    // ============================================================
    // POSTMAN CLIENT (Client Credentials)
    // ============================================================
    private async Task SeedPostmanClientAsync(IOpenIddictApplicationManager manager, CancellationToken ct)
    {
        const string clientId = "postman-client";
        const string clientSecret = "dev-postman-secret";

        if (await manager.FindByClientIdAsync(clientId, ct) is not null)
        {
            Logger.LogInformation("Postman client already exists, skipping.");
            return;
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = "Postman / External Tool Client",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            Permissions =
            {
                // Endpoints
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Revocation,

                // Client credentials flow
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,

                // API scope
                OpenIddictConstants.Permissions.Prefixes.Scope + "api"
            }
        };

        await manager.CreateAsync(descriptor, ct);
        Logger.LogInformation("Seeded Postman client: {ClientId}", clientId);
    }
}
