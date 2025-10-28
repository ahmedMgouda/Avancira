namespace Avancira.BFF.Services;

/// <summary>
/// Helper methods for HTTP request analysis
/// </summary>
public static class RequestHelper
{
    /// <summary>
    /// Determines if request is from API/AJAX (should get JSON response)
    /// vs browser navigation (should get redirects)
    /// </summary>
    public static bool IsApiRequest(HttpRequest request)
    {
        var path = request.Path;

        // /bff/* endpoints are API calls except auth flows
        if (path.StartsWithSegments("/bff", StringComparison.OrdinalIgnoreCase))
        {
            var authPaths = new[]
            {
                "/bff/auth/login",
                "/bff/signin-oidc",
                "/bff/signout-oidc",
                "/bff/signout-callback-oidc"
            };

            return !authPaths.Any(p =>
                path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
        }

        // Check Accept header
        if (request.Headers.Accept.Any(h => h?.Contains("application/json") == true))
            return true;

        // Check X-Requested-With header (AJAX)
        if (request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return true;

        return false;
    }
}