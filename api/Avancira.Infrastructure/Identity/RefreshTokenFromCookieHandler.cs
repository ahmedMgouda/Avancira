using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;
using Avancira.Infrastructure.Auth;
using Microsoft.AspNetCore;
using OpenIddict.Abstractions;

namespace Avancira.Infrastructure.Identity;

public sealed class RefreshTokenFromCookieHandler : IOpenIddictServerHandler<ProcessAuthenticationContext>
{
    public ValueTask HandleAsync(ProcessAuthenticationContext context)
    {
        var httpContext = context.Transaction.GetHttpRequest()?.HttpContext;
        if (httpContext is null)
            return default;

        if (context.Request.GrantType == OpenIddictConstants.GrantTypes.RefreshToken &&
            string.IsNullOrEmpty(context.Request.RefreshToken))
        {
            if (httpContext.Request.Cookies.TryGetValue(AuthConstants.Cookies.RefreshToken, out var cookie))
            {
                context.Request.RefreshToken = cookie;
            }
        }

        return default;
    }
}
