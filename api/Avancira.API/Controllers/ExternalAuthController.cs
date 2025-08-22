using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Avancira.Application.Auth;
using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Infrastructure.Identity.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

public enum ExternalAuthProvider
{
    Google,
    Facebook
}

[Route("api/auth")]
public class ExternalAuthController : BaseApiController
{
    private readonly IExternalAuthService _externalAuthService;
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IClientInfoService _clientInfoService;

    public ExternalAuthController(
        IExternalAuthService externalAuthService,
        UserManager<User> userManager,
        ITokenService tokenService,
        IClientInfoService clientInfoService)
    {
        _externalAuthService = externalAuthService;
        _userManager = userManager;
        _tokenService = tokenService;
        _clientInfoService = clientInfoService;
    }

    [HttpPost("external-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var provider = request.Provider!.Value.ToString();
        var result = await _externalAuthService.ValidateTokenAsync(provider, request.Token);

        if (!result.Succeeded || result.LoginInfo is null)
            return Unauthorized(result.Error);

        var info = result.LoginInfo;

        var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (user is null)
        {
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new User
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty
                };
                await _userManager.CreateAsync(user);
            }

            await _userManager.AddLoginAsync(user, info);
        }

        var clientInfo = await _clientInfoService.GetClientInfoAsync();
        var tokens = await _tokenService.GenerateTokenForUserAsync(user.Id, clientInfo, cancellationToken);

        SetRefreshTokenCookie(tokens.RefreshToken, tokens.RefreshTokenExpiryTime);

        return Ok(new TokenResponse(tokens.Token));
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expires)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth"
        };

        cookieOptions.Expires = expires;

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    public class ExternalLoginRequest
    {
        [Required]
        [EnumDataType(typeof(ExternalAuthProvider))]
        public ExternalAuthProvider? Provider { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
