using Avancira.Infrastructure.Auth;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Avancira.Infrastructure.Identity;


public sealed class LoginRedirectHandler : IOpenIddictServerHandler<HandleAuthorizationRequestContext>
{
    private readonly ILogger<LoginRedirectHandler> _logger;

    public LoginRedirectHandler(ILogger<LoginRedirectHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask HandleAsync(HandleAuthorizationRequestContext context)
    {
        var httpContext = context.Transaction.GetHttpRequest()?.HttpContext;
        if (httpContext is null)
        {
            _logger.LogWarning("No HTTP context available in authorization request");
            return;
        }

        try
        {
            var result = await httpContext.AuthenticateAsync(AuthConstants.Cookies.IdentityExchange);

            if (result.Succeeded && result.Principal is not null)
            {
                _logger.LogDebug("User already authenticated, proceeding with authorization");
                context.Principal = result.Principal;
                context.Principal.SetScopes(context.Request.GetScopes());
                return;
            }

            // User not authenticated, redirect to login
            var returnUrl = httpContext.Request.Path + httpContext.Request.QueryString;
            var loginUrl = $"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}";

            _logger.LogDebug("Redirecting unauthenticated user to login: {LoginUrl}", loginUrl);
            httpContext.Response.Redirect(loginUrl);
            context.HandleRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in login redirect handler");
            throw;
        }
    }
}
