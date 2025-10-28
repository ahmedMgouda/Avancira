namespace Avancira.BFF.Services;

using System.Security.Claims;
using Avancira.BFF.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Handles cookie authentication events
/// KEY FEATURE: Filters claims to keep cookie minimal (sub + sid only)
/// </summary>
public static class CookieEventHandlers
{
    /// <summary>
    /// Creates cookie authentication events
    /// </summary>
    public static CookieAuthenticationEvents CreateEvents(BffSettings settings)
    {
        return new CookieAuthenticationEvents
        {
            OnSigningIn = context => OnSigningIn(context, settings),
            OnRedirectToLogin = OnRedirectToLogin,
            OnRedirectToAccessDenied = OnRedirectToAccessDenied
        };
    }

    /// <summary>
    /// CRITICAL: Filters claims before storing in cookie
    /// Keeps only essential claims (sub + sid) to minimize cookie size
    /// 
    /// WHY: Cookie size directly impacts:
    /// - Network bandwidth (sent with every request)
    /// - Mobile performance
    /// - Browser limitations (4KB max)
    /// </summary>
    private static Task OnSigningIn(
        CookieSigningInContext context,
        BffSettings settings)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
            return Task.CompletedTask;

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        // Keep only essential claims
        var essentialClaimTypes = new HashSet<string>(settings.EssentialClaims);

        var claimsToRemove = identity.Claims
            .Where(c => !essentialClaimTypes.Contains(c.Type))
            .ToList();

        foreach (var claim in claimsToRemove)
        {
            identity.RemoveClaim(claim);
        }

        logger.LogDebug(
            "Cookie optimized: Removed {RemovedCount} claims, kept {KeptCount} essential claims ({Claims})",
            claimsToRemove.Count,
            identity.Claims.Count(),
            string.Join(", ", settings.EssentialClaims));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles redirect to login for API requests
    /// Returns 401 JSON instead of HTML redirect
    /// </summary>
    private static Task OnRedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (RequestHelper.IsApiRequest(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsJsonAsync(new
            {
                error = "unauthorized",
                message = "Authentication required"
            });
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles redirect to access denied for API requests
    /// Returns 403 JSON instead of HTML redirect
    /// </summary>
    private static Task OnRedirectToAccessDenied(
        RedirectContext<CookieAuthenticationOptions> context)
    {
        if (RequestHelper.IsApiRequest(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsJsonAsync(new
            {
                error = "forbidden",
                message = "Access denied"
            });
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
}

