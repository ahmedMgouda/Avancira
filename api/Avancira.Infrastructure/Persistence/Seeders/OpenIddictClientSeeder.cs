using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Avancira.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds OpenIddict application clients and scopes.
/// </summary>
public sealed class OpenIddictClientSeeder : ISeeder
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly ILogger<OpenIddictClientSeeder> _logger;

    public string Name => nameof(OpenIddictClientSeeder);

    public OpenIddictClientSeeder(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        ILogger<OpenIddictClientSeeder> logger)
    {
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting {Seeder}...", Name);

        await RegisterApplicationsAsync(cancellationToken);
        await RegisterScopesAsync(cancellationToken);

        _logger.LogInformation("{Seeder} completed successfully.", Name);
    }

    // ------------------------------------------------------------------------
    // Applications
    // ------------------------------------------------------------------------
    private async Task RegisterApplicationsAsync(CancellationToken ct)
    {
        // === 1. Resource Server (API) ===
        if (await _applicationManager.FindByClientIdAsync("resource_server", ct) is null)
        {
            var apiDescriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "resource_server",
                ClientSecret = "846B62D0-DEF9-4215-A99D-86E6B8DAB342",
                Permissions = { Permissions.Endpoints.Introspection }
            };

            await _applicationManager.CreateAsync(apiDescriptor, ct);
            _logger.LogInformation("Created OpenIddict client: {ClientId}", apiDescriptor.ClientId);
        }

        // === 2. BFF Client ===
        if (await _applicationManager.FindByClientIdAsync("bff-client", ct) is null)
        {
            var bffDescriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "bff-client",
                ClientSecret = "dev-bff-secret",
                DisplayName = "Avancira BFF (PKCE)",
                ClientType = ClientTypes.Confidential,
                ConsentType = ConsentTypes.Explicit,

                RedirectUris =
                {
                    new Uri("https://localhost:9200/bff/signin-oidc")
                },
                PostLogoutRedirectUris =
                {
                    new Uri("https://localhost:9200/bff/signout-callback-oidc"),
                    new Uri("https://localhost:4200/")
                },
                Permissions =
                {
                    // Endpoints
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.Revocation,
                    Permissions.Endpoints.EndSession,
                    Permissions.Endpoints.Introspection,

                    // Grant types
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,

                    // Response types
                    Permissions.ResponseTypes.Code,

                    // Scopes
                    Permissions.Prefixes.Scope + Scopes.OpenId,
                    Permissions.Prefixes.Scope + Scopes.Profile,
                    Permissions.Prefixes.Scope + Scopes.Email,
                    Permissions.Prefixes.Scope + Scopes.Roles,
                    Permissions.Prefixes.Scope + Scopes.OfflineAccess,
                    Permissions.Prefixes.Scope + "api"
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            };

            await _applicationManager.CreateAsync(bffDescriptor, ct);
            _logger.LogInformation("Created OpenIddict client: {ClientId}", bffDescriptor.ClientId);
        }

        // === 3. Postman / External Client ===
        if (await _applicationManager.FindByClientIdAsync("postman-client", ct) is null)
        {
            var postmanDescriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "postman-client",
                ClientSecret = "dev-postman-secret",
                DisplayName = "Postman / External Tool Client",
                ClientType = ClientTypes.Confidential,
                ConsentType = ConsentTypes.Implicit,
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.Revocation,
                    Permissions.GrantTypes.ClientCredentials,
                    Permissions.Prefixes.Scope + "api"
                }
            };

            await _applicationManager.CreateAsync(postmanDescriptor, ct);
            _logger.LogInformation("Created OpenIddict client: {ClientId}", postmanDescriptor.ClientId);
        }
    }

    // ------------------------------------------------------------------------
    // Scopes
    // ------------------------------------------------------------------------
    private async Task RegisterScopesAsync(CancellationToken ct)
    {
        if (await _scopeManager.FindByNameAsync("api", ct) is null)
        {
            var apiScope = new OpenIddictScopeDescriptor
            {
                Name = "api",
                DisplayName = "Avancira API access",
                Resources = { "resource_server" }
            };

            await _scopeManager.CreateAsync(apiScope, ct);
            _logger.LogInformation("Created OpenIddict scope: {Scope}", apiScope.Name);
        }
    }
}
