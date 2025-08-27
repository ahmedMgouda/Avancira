using OpenIddict.Abstractions;
using OpenIddict.Server;
using Avancira.Application.Identity;
using Microsoft.Extensions.Logging;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Avancira.Infrastructure.Identity;

public class PasswordGrantHandler : IOpenIddictServerHandler<HandleTokenRequestContext>
{
    private readonly IUserAuthenticationService _userAuthenticationService;
    private readonly ILogger<PasswordGrantHandler> _logger;

    public PasswordGrantHandler(
        IUserAuthenticationService userAuthenticationService,
        ILogger<PasswordGrantHandler> logger)
    {
        _userAuthenticationService = userAuthenticationService;
        _logger = logger;
    }

    public async ValueTask HandleAsync(HandleTokenRequestContext context)
    {
        if (!context.Request.IsPasswordGrantType())
        {
            return;
        }

        if (string.IsNullOrEmpty(context.Request.Scope))
        {
            _logger.LogWarning("Token request missing scope parameter.");
            context.Reject(OpenIddictConstants.Errors.InvalidRequest,
                "The 'scope' parameter is required.");
            return;
        }

        var scopes = context.Request.GetScopes();
        if (!scopes.Contains(OpenIddictConstants.Scopes.OpenId))
        {
            _logger.LogWarning("Token request missing required 'openid' scope.");
            context.Reject(OpenIddictConstants.Errors.InvalidScope,
                "The required 'openid' scope is missing.");
            return;
        }

        var email = context.Request.Username;
        var password = context.Request.Password;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("Token request missing username or password.");
            context.Reject(OpenIddictConstants.Errors.InvalidGrant,
                "The username or password cannot be empty.");
            return;
        }

        var user = await _userAuthenticationService.ValidateCredentialsAsync(email, password);
        if (user is null)
        {
            _logger.LogWarning("Invalid credentials for user {Email}.", email);
            context.Reject(OpenIddictConstants.Errors.InvalidGrant,
                "The username or password is invalid.");
            return;
        }

        var principal = await _userAuthenticationService.CreatePrincipalAsync(user);
        principal.SetScopes(scopes);

        context.Principal = principal;
        context.HandleRequest();
    }
}
