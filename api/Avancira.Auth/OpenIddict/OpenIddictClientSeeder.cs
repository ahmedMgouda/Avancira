using Avancira.Infrastructure.Common;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using System.Globalization;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Avancira.Auth.OpenIddict;

/// <summary>
/// Seeds OpenId application clients (BFF, Postman, etc.)
/// </summary>
internal sealed class OpenIddictClientSeeder(
    ILogger<OpenIddictClientSeeder> logger,
    IOpenIddictApplicationManager? manager = null,
    IOpenIddictScopeManager? scopeManager = null
) : BaseSeeder<OpenIddictClientSeeder>(logger)
{
    private readonly IOpenIddictApplicationManager? _manager = manager;
    private readonly IOpenIddictScopeManager? _scopeManager = scopeManager;
    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting OpenIddict application seeding...");

        await RegisterApplicationsAsync(_manager!);
        await RegisterScopesAsync(_scopeManager!);

        Logger.LogInformation("OpenIddict application seeding completed");
    }

    private async Task RegisterApplicationsAsync(IOpenIddictApplicationManager manager)
    {
        // API
        if (await manager.FindByClientIdAsync("resource_server") == null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "resource_server",
                ClientSecret = "846B62D0-DEF9-4215-A99D-86E6B8DAB342",
                Permissions =
                    {
                        Permissions.Endpoints.Introspection
                    }
            };

            await manager.CreateAsync(descriptor);
        }

        // BFF
        if (await manager.FindByClientIdAsync("bff-client") is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "bff-client",
                ClientSecret = "dev-bff-secret",
                DisplayName = "Avancira PKCE",
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
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                        Permissions.Endpoints.Revocation,
                    Permissions.Endpoints.EndSession,


                    // Grant types
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                        Permissions.Endpoints.Introspection,

                    // Response types
                    Permissions.ResponseTypes.Code,

                    // Scopes
                    Permissions.Prefixes.Scope + Scopes.OpenId,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + Scopes.OfflineAccess,
                    Permissions.Prefixes.Scope + "api"
                },
                    Requirements =
                    {
                        // PKCE required
                        OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
                    }
                };

            await manager.CreateAsync(descriptor);
        }

        // Postman / External Tool
        if (await manager.FindByClientIdAsync("postman-client") is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "postman-client",
                ClientSecret = "dev-postman-secret",
                DisplayName = "Postman / External Tool Client",
                ClientType = OpenIddictConstants.ClientTypes.Confidential,
                ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
                Permissions =
            {
                // Endpoints
                Permissions.Endpoints.Token,
                Permissions.Endpoints.Revocation,

                // Client credentials flow
                Permissions.GrantTypes.ClientCredentials,

                // API scope
                Permissions.Prefixes.Scope + "api"
            }
            };

            await manager.CreateAsync(descriptor);
        }

    }
    private async Task RegisterScopesAsync(IOpenIddictScopeManager manager)
    {
        if (await manager.FindByNameAsync("api") is null)
        {
            await manager.CreateAsync(new OpenIddictScopeDescriptor
            {
                DisplayName = "Avancira API access",
                Name = "api",
                Resources =
                    {
                        "resource_server"
                    }
            });
        }
    }
}
