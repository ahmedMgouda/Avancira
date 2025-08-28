using Microsoft.AspNetCore.Http;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Avancira.Infrastructure.Identity;

public sealed class LoginRedirectHandler(IHttpContextAccessor http) : IOpenIddictServerHandler<HandleAuthorizationRequestContext>
{
    private readonly IHttpContextAccessor _http = http;

    public ValueTask HandleAsync(HandleAuthorizationRequestContext context)
    {
        var httpContext = _http.HttpContext;
        if (httpContext is null)
        {
            return ValueTask.CompletedTask;
        }

        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            return ValueTask.CompletedTask;
        }

        var returnUrl = httpContext.Request.PathBase.Add(httpContext.Request.Path).Value + httpContext.Request.QueryString.Value;
        httpContext.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        context.HandleRequest();
        return ValueTask.CompletedTask;
    }
}

