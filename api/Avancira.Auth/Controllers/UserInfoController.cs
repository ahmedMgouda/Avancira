using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Avancira.Infrastructure.Identity.Users;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Avancira.Auth.Controllers;

/// <summary>
/// OpenID Connect–compliant UserInfo endpoint.
/// Returns live user profile data (with roles when authorized).
/// </summary>
[ApiController]
[Route("connect")]
public sealed class UserInfoController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UserInfoController> _logger;

    public UserInfoController(UserManager<User> userManager, ILogger<UserInfoController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("userinfo")]
    [HttpPost("userinfo")]
    [Produces("application/json")]
    public async Task<IActionResult> GetUserInfo(CancellationToken cancellationToken)
    {
        var subject = User.GetClaim(Claims.Subject);

        if (string.IsNullOrEmpty(subject))
        {
            _logger.LogWarning("No subject claim found in userinfo request.");
            return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var user = await _userManager.FindByIdAsync(subject);

        if (user is null)
        {
            _logger.LogWarning("User {Subject} not found for userinfo request.", subject);
            return Challenge(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The specified access token is bound to an account that no longer exists."
                }));
        }

        var claims = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [Claims.Subject] = user.Id
        };

        if (User.HasScope(Scopes.Profile))
        {
            claims[Claims.Name] = user.UserName;
            if (!string.IsNullOrEmpty(user.FirstName))
                claims[Claims.GivenName] = user.FirstName;
            if (!string.IsNullOrEmpty(user.LastName))
                claims[Claims.FamilyName] = user.LastName;
            if (user.ProfileImageUrl is not null)
                claims[Claims.Picture] = user.ProfileImageUrl.ToString();
        }

        if (User.HasScope(Scopes.Email))
        {
            claims[Claims.Email] = user.Email ?? string.Empty;
            claims[Claims.EmailVerified] = user.EmailConfirmed;
        }

 
        if (User.HasScope(Scopes.Phone))
        {
            claims[Claims.PhoneNumber] = user.PhoneNumber;
            claims[Claims.PhoneNumberVerified] = user.PhoneNumberConfirmed;
        }

        if (User.HasScope(Scopes.Roles))
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count > 0)
            {
                claims[Claims.Role] = roles.ToArray();
            }
        }

        _logger.LogDebug("UserInfo returned for user {UserId}.", user.Id);
        return Ok(claims);
    }
}
