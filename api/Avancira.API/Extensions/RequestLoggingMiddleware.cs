using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Avancira.API.Middleware;

/// <summary>
/// Simple debugging middleware to log all incoming requests with headers and tokens
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        // Extract Authorization header
        var authHeader = request.Headers.Authorization.ToString();
        var token = !string.IsNullOrEmpty(authHeader)
            ? authHeader.Replace("Bearer ", "").Substring(0, Math.Min(30, authHeader.Length)) + "..."
            : "[NONE]";

        _logger.LogInformation(
            "REQUEST: {Method} {Path} | Auth: {Auth} | Token: {Token}",
            request.Method,
            request.Path,
            authHeader.StartsWith("Bearer") ? "YES" : "NO",
            token);

        // Log all request headers
        foreach (var header in request.Headers)
        {
            _logger.LogInformation("  Header: {Key} = {Value}", header.Key, string.Join(", ", header.Value.ToArray()));
        }

        // Log user info if authenticated
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value ?? "unknown";
            _logger.LogInformation("  Authenticated User: {UserId}", userId);
        }
        else
        {
            _logger.LogInformation("  User: [ANONYMOUS]");
        }

        await _next(context);

        // Log response status
        _logger.LogInformation("RESPONSE: {Method} {Path} => {StatusCode}",
            request.Method,
            request.Path,
            context.Response.StatusCode);
    }
}

/// <summary>
/// Extension method to register the middleware
/// </summary>
public static class RequestLoggingExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}