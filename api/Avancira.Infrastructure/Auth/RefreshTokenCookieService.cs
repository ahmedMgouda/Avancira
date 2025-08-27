using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Avancira.Infrastructure.Auth;

public class RefreshTokenCookieService : IRefreshTokenCookieService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _environment;

    public RefreshTokenCookieService(IHttpContextAccessor httpContextAccessor, IHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
    }

    public void SetRefreshTokenCookie(string refreshToken, DateTime? expires)
    {
        var context = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext available");

        var options = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            Secure = !_environment.IsDevelopment()
        };

        if (expires.HasValue)
        {
            options.Expires = expires.Value;
        }

        context.Response.Cookies.Append("refreshToken", refreshToken, options);
    }
}

