using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Server.Events;

namespace Avancira.Infrastructure.Identity;

public sealed class ExternalLoginHandler : OpenIddictServerAspNetCoreHandler<HandleAuthorizationRequestContext>
{
    public override async ValueTask HandleAsync(HandleAuthorizationRequestContext context)
    {
        // Try to authenticate using the OpenIddict server scheme to check
        // whether the external provider already returned a principal.
        var result = await context.HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (result.Succeeded && result.Principal is not null)
        {
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationType);

            var subject = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.NewGuid().ToString();
            identity.SetClaim(OpenIddictConstants.Claims.Subject, subject);

            var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(name))
            {
                identity.SetClaim(OpenIddictConstants.Claims.Name, name);
            }

            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                identity.SetClaim(OpenIddictConstants.Claims.Email, email);
            }

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(context.Request.GetScopes());

            context.Principal = principal;
            context.HandleRequest();
            return;
        }

        // No existing principal found, trigger a challenge to the requested provider.
        var provider = context.HttpContext.Request.Query["provider"].ToString();
        if (!string.IsNullOrEmpty(provider))
        {
            await context.HttpContext.ChallengeAsync(provider);
            context.HandleRequest();
        }
    }
}

