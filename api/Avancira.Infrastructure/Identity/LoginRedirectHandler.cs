using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Server;
using static Avancira.Infrastructure.Auth.AuthConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Avancira.Infrastructure.Identity;

public sealed class LoginRedirectHandler : IOpenIddictServerHandler<HandleAuthorizationRequestContext>
{
    public async ValueTask HandleAsync(HandleAuthorizationRequestContext context)
    {
        var httpContext = context.Transaction.GetHttpRequest()?.HttpContext;
        if (httpContext is null)
            return;

        var result = await httpContext.AuthenticateAsync(Cookies.IdentityExchange);
        if (result.Succeeded && result.Principal is not null)
        {
            context.Principal = result.Principal;
            return;
        }

        var returnUrl = httpContext.Request.Path + httpContext.Request.QueryString;
        httpContext.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
        context.HandleRequest();
    }
}
