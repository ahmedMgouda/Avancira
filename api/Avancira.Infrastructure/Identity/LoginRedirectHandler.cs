using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using System.Security.Claims;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Avancira.Infrastructure.Identity;

public sealed class LoginRedirectHandler : IOpenIddictServerHandler<HandleAuthorizationRequestContext>
{
    private readonly IHttpContextAccessor _http;

    public LoginRedirectHandler(IHttpContextAccessor http) => _http = http;

    public async ValueTask HandleAsync(HandleAuthorizationRequestContext context)
    {
        var httpContext = _http.HttpContext;
        if (httpContext is null)
            return;

        // Try to authenticate using the Identity cookie
        var result = await httpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (result.Succeeded && result.Principal is not null)
        {
            // Build a claims identity we can ensure has `sub`
            var src = result.Principal;
            var identity = new ClaimsIdentity(src.Identity, src.Claims);

            // Ensure subject (sub) is present (OpenIddict requires this)
            var subject = identity.FindFirst(OpenIddictConstants.Claims.Subject)?.Value
                       ?? identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(subject))
            {
                // No way to identify the user -> force re-login
                httpContext.Response.Redirect("/Account/Login");
                context.HandleRequest();
                return;
            }

            if (identity.FindFirst(OpenIddictConstants.Claims.Subject) is null)
                identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, subject));

            // Optional: also ensure a display name
            if (identity.FindFirst(OpenIddictConstants.Claims.Name) is null)
            {
                var name = identity.FindFirst(ClaimTypes.Name)?.Value
                           ?? identity.FindFirst("name")?.Value
                           ?? subject;
                identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, name));
            }

            var principal = new ClaimsPrincipal(identity);

            // Propagate requested scopes to the principal so OpenIddict can issue them
            principal.SetScopes(context.Request.GetScopes());

            // Hand back to OpenIddict to finish the /connect/authorize flow
            context.Principal = principal;
            return; // IMPORTANT: don't call HandleRequest() here
        }

        // Not authenticated -> redirect to your login page
        var req = httpContext.Request;
        var returnUrl = req.PathBase.Add(req.Path).Value ?? string.Empty;
        if (req.QueryString.HasValue)
            returnUrl += req.QueryString.Value;

        httpContext.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        context.HandleRequest(); // We produced a 302 response, so short-circuit
    }
}
