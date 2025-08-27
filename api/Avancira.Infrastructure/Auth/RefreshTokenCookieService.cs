using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Avancira.Infrastructure.Auth;

public class RefreshTokenCookieService : IRefreshTokenCookieService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _environment;
    private readonly IOptions<CookieOptions> _cookieOptions;

    public RefreshTokenCookieService(
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment environment,
        IOptions<CookieOptions> cookieOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
        _cookieOptions = cookieOptions;
    }

    public void SetRefreshTokenCookie(string refreshToken, DateTime? expires)
    {
        var context = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext available");

        var sameSite = _cookieOptions.Value.SameSite == SameSiteMode.Unspecified
            ? SameSiteMode.Lax
            : _cookieOptions.Value.SameSite;

        var options = new CookieOptions
        {
            HttpOnly = true,
            SameSite = sameSite,
            Path = AuthConstants.Cookies.PathRoot,
            Secure = !_environment.IsDevelopment()
        };

        if (expires.HasValue)
        {
            options.Expires = expires.Value;
        }

        context.Response.Cookies.Append(AuthConstants.Cookies.RefreshToken, refreshToken, options);
    }
}

