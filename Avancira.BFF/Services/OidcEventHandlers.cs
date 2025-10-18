namespace Avancira.BFF.Services;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// Handles OpenID Connect authentication events
/// </summary>
public static class OidcEventHandlers
{
    /// <summary>
    /// Creates OpenID Connect events
    /// </summary>
    public static OpenIdConnectEvents CreateEvents()
    {
        return new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = OnRedirectToIdentityProvider,
            OnTokenValidated = OnTokenValidated,
            OnTokenResponseReceived = OnTokenResponseReceived,
            OnRemoteFailure = OnRemoteFailure,
            OnAuthenticationFailed = OnAuthenticationFailed
        };
    }

    private static Task OnRedirectToIdentityProvider(RedirectContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Redirecting to identity provider");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when token is validated
    /// Logs user info for debugging
    /// </summary>
    private static Task OnTokenValidated(TokenValidatedContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        var sub = context.Principal?.FindFirst("sub")?.Value;
        var sid = context.Principal?.FindFirst("sid")?.Value;
        var claimCount = context.Principal?.Claims.Count() ?? 0;

        logger.LogInformation(
            "Token validated - Sub: {Sub}, Sid: {Sid}, Claims: {ClaimCount}",
            sub,
            sid,
            claimCount);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when tokens are received from auth server
    /// Confirms server-side storage strategy
    /// </summary>
    private static Task OnTokenResponseReceived(TokenResponseReceivedContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogDebug("Tokens received from auth server (will be stored server-side by Duende)");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles remote authentication failures
    /// Returns JSON error for API requests
    /// </summary>
    private static Task OnRemoteFailure(RemoteFailureContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogError(context.Failure, "Remote authentication failure");

        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(new
        {
            error = "authentication_failed",
            message = context.Failure?.Message ?? "Remote authentication failed"
        });
    }

    /// <summary>
    /// Handles local authentication failures
    /// </summary>
    private static Task OnAuthenticationFailed(AuthenticationFailedContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogError(context.Exception, "Authentication failed");

        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(new
        {
            error = "authentication_failed",
            message = context.Exception.Message
        });
    }
}