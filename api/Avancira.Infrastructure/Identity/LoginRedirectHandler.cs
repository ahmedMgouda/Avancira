using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using System.Security.Claims;
using static Avancira.Infrastructure.Auth.AuthConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Avancira.Infrastructure.Identity;

public sealed class LoginRedirectHandler : IOpenIddictServerHandler<HandleAuthorizationRequestContext>
{
    public async ValueTask HandleAsync(HandleAuthorizationRequestContext context)
    {
        var request = context.Transaction.GetHttpRequest();
        var response = request?.HttpContext.Response;
        var httpContext = request?.HttpContext;

        if (httpContext is null || response is null)
            return;

        // Try authenticate via short-lived cookie
        var result = await httpContext.AuthenticateAsync(Cookies.IdentityExchange);
        if (result.Succeeded && result.Principal is not null)
        {
            var identity = (ClaimsIdentity)result.Principal.Identity!;

            // Ensure subject (sub)
            var subject = result.Principal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value
                       ?? result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(subject))
            {
                response.Redirect("/Account/Login");
                context.HandleRequest();
                return;
            }

            if (identity.FindFirst(OpenIddictConstants.Claims.Subject) is null)
                identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, subject));

            if (identity.FindFirst(OpenIddictConstants.Claims.Name) is null)
            {
                var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value
                           ?? subject;
                identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, name));
            }

            var principal = new ClaimsPrincipal(identity);

            // Safer: restrict scopes to supported ones
            principal.SetScopes(context.Request.GetScopes());
         

            context.Principal = principal;

            // Clear the bridge cookie (one-time use)
            await httpContext.SignOutAsync(Cookies.IdentityExchange);
            return;
        }

        var returnUrl = request?.Path + request?.QueryString;
        response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
        context.HandleRequest();
    }
}
