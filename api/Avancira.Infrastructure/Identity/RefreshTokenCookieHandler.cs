using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;
using Avancira.Infrastructure.Auth;

namespace Avancira.Infrastructure.Identity;

public sealed class RefreshTokenCookieHandler : IOpenIddictServerHandler<ApplyTokenResponseContext>
{
    private readonly IHostEnvironment _environment;
    private readonly IOptions<CookieOptions> _cookieOptions;

    public RefreshTokenCookieHandler(IHostEnvironment environment, IOptions<CookieOptions> cookieOptions)
    {
        _environment = environment;
        _cookieOptions = cookieOptions;
    }

    public ValueTask HandleAsync(ApplyTokenResponseContext context)
    {
        var refreshToken = context.Response?.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken))
        {
            return ValueTask.CompletedTask;
        }

        var httpContext = context.Transaction?.HttpContext;
        if (httpContext is null)
        {
            return ValueTask.CompletedTask;
        }

        var sameSite = _cookieOptions.Value.SameSite == SameSiteMode.Unspecified
            ? SameSiteMode.Lax
            : _cookieOptions.Value.SameSite;

        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = sameSite,
            Path = AuthConstants.Cookies.PathRoot
        };

        httpContext.Response.Cookies.Append(AuthConstants.Cookies.RefreshToken, refreshToken, options);
        return ValueTask.CompletedTask;
    }
}
