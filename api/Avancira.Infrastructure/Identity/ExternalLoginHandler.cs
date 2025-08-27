using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Avancira.Infrastructure.Identity;

public sealed class ExternalLoginHandler
    : IOpenIddictServerHandler<HandleAuthorizationRequestContext>
{
    private readonly IHttpContextAccessor _http;

    public ExternalLoginHandler(IHttpContextAccessor http) => _http = http;

    public async ValueTask HandleAsync(HandleAuthorizationRequestContext context)
    {
        var http = _http.HttpContext;
        if (http is null) return;

        var result = await http.AuthenticateAsync(IdentityConstants.ExternalScheme);

        if (result.Succeeded && result.Principal is not null)
        {
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            var sub = result.Principal.FindFirstValue(OpenIddictConstants.Claims.Subject)
                   ?? result.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? Guid.NewGuid().ToString("N");
            identity.SetClaim(OpenIddictConstants.Claims.Subject, sub);

            var name = result.Principal.FindFirstValue(OpenIddictConstants.Claims.Name)
                    ?? result.Principal.FindFirstValue(ClaimTypes.Name);
            if (!string.IsNullOrWhiteSpace(name))
                identity.SetClaim(OpenIddictConstants.Claims.Name, name);

            var email = result.Principal.FindFirstValue(OpenIddictConstants.Claims.Email)
                     ?? result.Principal.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrWhiteSpace(email))
                identity.SetClaim(OpenIddictConstants.Claims.Email, email);

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(context.Request.GetScopes());

            context.Principal = principal;
            context.HandleRequest();

            await http.SignOutAsync(IdentityConstants.ExternalScheme);
            return;
        }

        var provider = http.Request.Query["provider"].ToString();
        if (!string.IsNullOrWhiteSpace(provider))
        {
            var returnUrl = http.Request.PathBase.Add(http.Request.Path).Value + http.Request.QueryString.Value;
            var props = new AuthenticationProperties { RedirectUri = returnUrl };

            await http.ChallengeAsync(provider, props);
            context.HandleRequest();
        }
    }
}
