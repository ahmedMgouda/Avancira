namespace Avancira.BFF.Services;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

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
          //  OnRedirectToIdentityProvider = OnRedirectToIdentityProvider,
            OnTokenValidated = OnTokenValidated,
            OnTokenResponseReceived = OnTokenResponseReceived,
            OnRemoteFailure = OnRemoteFailure,
            OnAuthenticationFailed = OnAuthenticationFailed,
            OnRemoteSignOut = OnRemoteSignOut,
         //   OnSignedOutCallbackRedirect = OnSignedOutCallbackRedirect
        };
    }

    /// <summary>
    /// Called when redirecting to identity provider
    /// Ensures post_logout_redirect_uri is set for logout requests
    /// </summary>
    private static Task OnRedirectToIdentityProvider(RedirectContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        // Check if this is a logout request
        if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Logout)
        {
            // Ensure we have the callback URI set
            var logoutCallbackUri = $"{context.Request.Scheme}://{context.Request.Host}/bff/signout-callback-oidc";
            context.ProtocolMessage.PostLogoutRedirectUri = logoutCallbackUri;

            logger.LogInformation(
                "Redirecting to Auth logout - PostLogoutRedirectUri: {Uri}",
                logoutCallbackUri);
        }
        else
        {
            logger.LogDebug("Redirecting to identity provider for authentication");
        }

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
    /// Called when the middleware is about to redirect after signout callback
    /// This happens AFTER Auth server redirects back to /bff/signout-callback-oidc
    /// </summary>
    private static Task OnSignedOutCallbackRedirect(RemoteSignOutContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

       // Get the target redirect URI(should be your SPA)
        var redirectUri = context.Properties?.RedirectUri
            ?? context.Options.SignedOutRedirectUri
            ?? "https://localhost:4200/";

        logger.LogInformation(
            "Signout callback complete - Redirecting to: {Uri}",
            redirectUri);

        //Default behavior will redirect to the URI
        //You can override here if needed:
         context.Response.Redirect("https://localhost:4200/");
         context.HandleResponse();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a remote signout is triggered (back-channel logout)
    /// This can happen when user logs out from Auth server directly
    /// or from another BFF instance
    /// </summary>
    private static Task OnRemoteSignOut(RemoteSignOutContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Remote signout triggered (back-channel logout)");

        // The middleware will handle the signout
        // You can perform additional cleanup here if needed

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