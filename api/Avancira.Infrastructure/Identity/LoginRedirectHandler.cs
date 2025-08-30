using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Server;
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

        if (httpContext.User?.Identity?.IsAuthenticated == true)
            return;


        var result = await httpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (result.Succeeded)
        {
            httpContext.User = result.Principal!;
            return;
        }

        var req = httpContext.Request;
        var returnUrl = req.PathBase.Add(req.Path).Value ?? string.Empty;
        if (req.QueryString.HasValue)
            returnUrl += req.QueryString.Value;

        httpContext.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        context.HandleRequest();
    }
}
