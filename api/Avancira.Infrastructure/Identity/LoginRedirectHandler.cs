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

        var request = httpContext.Request;
        var returnUrl = request.PathBase.Add(request.Path).Value ?? string.Empty;
        if (request.QueryString.HasValue)
        {
            returnUrl += request.QueryString.Value;
        }

        httpContext.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        context.HandleRequest();
        return ValueTask.CompletedTask;
    }
}

