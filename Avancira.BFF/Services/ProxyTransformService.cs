namespace Avancira.BFF.Services;

using Duende.AccessTokenManagement.OpenIdConnect;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;

/// <summary>
/// YARP proxy transform handlers
/// KEY FEATURES:
/// 1. Automatic access token injection
/// 2. Security header cleanup
/// </summary>
public static class ProxyTransformService
{
    /// <summary>
    /// Attaches access token to proxied API requests
    /// 
    /// FLOW:
    /// 1. Check if user is authenticated
    /// 2. Retrieve access token from Duende (using sub + sid)
    /// 3. Attach as Bearer token to outgoing request
    /// </summary>
    public static async ValueTask AttachAccessToken(RequestTransformContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            return;

        var tokenManager = context.HttpContext.RequestServices
            .GetRequiredService<IUserTokenManager>();

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        try
        {
            var result = await tokenManager.GetAccessTokenAsync(context.HttpContext.User);

            if (result.WasSuccessful(out var token))
            {
                // token is of type UserToken
                context.ProxyRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token.AccessToken);
            }
            else
            {
                var error = result.FailedResult?.Error ?? "unknown";
                logger.LogWarning("Token retrieval failed: {Error}", error);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error attaching access token");
        }
    }

    /// <summary>
    /// Removes sensitive headers from proxied responses
    /// </summary>
    public static ValueTask CleanSecurityHeaders(ResponseTransformContext context)
    {
        var headersToRemove = new[]
        {
            "Set-Cookie",       // Don't leak API cookies
            "Server",           // Hide server info
            "X-Powered-By",     // Hide tech stack
            "X-AspNet-Version"  // Hide framework version
        };

        foreach (var header in headersToRemove)
        {
            context.ProxyResponse?.Headers.Remove(header);
        }

        return ValueTask.CompletedTask;
    }
}
