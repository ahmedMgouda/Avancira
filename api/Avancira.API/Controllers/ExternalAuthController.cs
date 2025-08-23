using System.Linq;
using System;
using Avancira.Application.Auth;
using Avancira.Application.Auth.Dtos;
using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Avancira.API.Controllers;

[Route("api/auth")]
public class ExternalAuthController : BaseApiController
{
    private readonly IExternalAuthService _externalAuthService;
    private readonly IExternalUserService _externalUserService;
    private readonly ITokenService _tokenService;
    private readonly IClientInfoService _clientInfoService;
    private readonly ILogger<ExternalAuthController> _logger;

    public ExternalAuthController(
        IExternalAuthService externalAuthService,
        IExternalUserService externalUserService,
        ITokenService tokenService,
        IClientInfoService clientInfoService,
        ILogger<ExternalAuthController> logger)
    {
        _externalAuthService = externalAuthService;
        _externalUserService = externalUserService;
        _tokenService = tokenService;
        _clientInfoService = clientInfoService;
        _logger = logger;
    }

    [HttpPost("external-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest request, CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("X-CSRF-TOKEN", out var csrfToken))
        {
            var origin = Request.Headers["Origin"].FirstOrDefault() ?? Request.Headers["Referer"].FirstOrDefault();
            if (string.IsNullOrEmpty(origin) ||
                !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                uri.Host != Request.Host.Host)
            {
                return Forbid();
            }
        }

        var result = await _externalAuthService.ValidateTokenAsync(request.Provider, request.Token);

        if (!result.Succeeded || result.LoginInfo is null)
        {
            if (result.ErrorType == ExternalAuthErrorType.UnsupportedProvider)
            {
                return BadRequest(result.Error);
            }

            _logger.LogWarning("External login validation failed: {Error}", result.Error);
            return Unauthorized("Invalid external login token");
        }

        var info = result.LoginInfo;

        var userResult = await _externalUserService.EnsureUserAsync(info);
        if (!userResult.Succeeded)
        {
            return userResult.ErrorType switch
            {
                ExternalUserError.Unauthorized => Unauthorized(),
                ExternalUserError.BadRequest => BadRequest(userResult.Error),
                ExternalUserError.Problem => Problem(userResult.Error),
                _ => Problem(userResult.Error)
            };
        }

        var clientInfo = await _clientInfoService.GetClientInfoAsync();
        var tokens = await _tokenService.GenerateTokenForUserAsync(userResult.UserId!, clientInfo, cancellationToken);

        SetRefreshTokenCookie(tokens.RefreshToken, tokens.RefreshTokenExpiryTime);

        return Ok(new TokenResponse(tokens.Token));
    }
}
