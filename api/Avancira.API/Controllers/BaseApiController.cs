using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Avancira.API.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Gets the current user's roles from JWT claims
    /// </summary>
    /// <returns>List of user roles</returns>
    protected IEnumerable<string> GetUserRoles()
    {
        return User.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }

    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    /// <param name="role">Role to check</param>
    /// <returns>True if user has the role, false otherwise</returns>
    protected bool HasRole(string role)
    {
        return User.IsInRole(role);
    }

    /// <summary>
    /// Sets the refresh token cookie with standard options
    /// </summary>
    /// <param name="refreshToken">Refresh token value</param>
    /// <param name="expires">Optional expiration time for persistent cookies</param>
    protected void SetRefreshTokenCookie(string refreshToken, DateTime? expires)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        };

        if (expires.HasValue)
        {
            cookieOptions.Expires = expires.Value;
        }
        var env = HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        cookieOptions.Secure = !env.IsDevelopment();

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}
