using Microsoft.AspNetCore.Http;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Avancira.Infrastructure.Identity;

public sealed class LoginRedirectHandler : IOpenIddictServerHandler<HandleAuthorizationRequestContext>
{
    private readonly IHttpContextAccessor _http;

    public LoginRedirectHandler(IHttpContextAccessor http) => _http = http;

    public ValueTask HandleAsync(HandleAuthorizationRequestContext context)
    {
        var httpContext = _http.HttpContext;
        if (httpContext is null)
            return ValueTask.CompletedTask;

        if (httpContext.User?.Identity?.IsAuthenticated == true)
            return ValueTask.CompletedTask;

        var req = httpContext.Request;
        var returnUrl = req.PathBase.Add(req.Path).Value ?? string.Empty;
        if (req.QueryString.HasValue)
            returnUrl += req.QueryString.Value;

        httpContext.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        context.HandleRequest();
        return ValueTask.CompletedTask;
    }
}
