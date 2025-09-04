using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;
using Avancira.Infrastructure.Auth;
using Microsoft.AspNetCore;

namespace Avancira.Infrastructure.Identity;

public sealed class RefreshTokenCookieHandler : IOpenIddictServerHandler<ApplyTokenResponseContext>
{
    private readonly IHostEnvironment _environment;

    public RefreshTokenCookieHandler(IHostEnvironment environment)
        => _environment = environment;

    public ValueTask HandleAsync(ApplyTokenResponseContext context)
    {
        // Only add cookie if a refresh token was actually issued
        if (string.IsNullOrEmpty(context.Response?.RefreshToken))
            return ValueTask.CompletedTask;

        var httpContext = context.Transaction.GetHttpRequest()?.HttpContext;
        if (httpContext is null)
            return ValueTask.CompletedTask;

        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = AuthConstants.Cookies.PathRoot,
            Expires = DateTimeOffset.UtcNow.AddDays(7) // match refresh token lifetime
        };

        httpContext.Response.Cookies.Append(
            AuthConstants.Cookies.RefreshToken,
            context.Response.RefreshToken!,
            options);

        // Optional: remove refresh token from JSON response
        context.Response.RefreshToken = null;

        return ValueTask.CompletedTask;
    }
}
