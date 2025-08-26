using Avancira.Application.Identity;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Application.Auth;
using Avancira.Application.Auth.Dtos;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IExternalAuthService _externalAuthService;
    private readonly IExternalUserService _externalUserService;

    public AuthController(
        IAuthenticationService authenticationService,
        IExternalAuthService externalAuthService,
        IExternalUserService externalUserService)
    {
        _authenticationService = authenticationService;
        _externalAuthService = externalAuthService;
        _externalUserService = externalUserService;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> GenerateToken([FromBody] TokenGenerationDto request)
    {
        var pair = await _authenticationService.GenerateTokenAsync(request);
        DateTime? expires = request.RememberMe ? pair.RefreshTokenExpiryTime : null;
        SetRefreshTokenCookie(pair.RefreshToken, expires);
        return Ok(new TokenResponse(pair.Token));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenDto? request)
    {
        var refreshToken = request?.Token ?? Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized();
        }

        var pair = await _authenticationService.RefreshTokenAsync(refreshToken);
        SetRefreshTokenCookie(pair.RefreshToken, pair.RefreshTokenExpiryTime);
        return Ok(new TokenResponse(pair.Token));
    }

    [HttpPost("external-login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> ExternalLogin([FromBody] ExternalLoginRequest request)
    {
        var authResult = await _externalAuthService.ValidateTokenAsync(request.Provider, request.Token);
        if (!authResult.Succeeded)
        {
            return authResult.ErrorType switch
            {
                ExternalAuthErrorType.InvalidToken or ExternalAuthErrorType.UnverifiedEmail => Unauthorized(authResult.Error),
                ExternalAuthErrorType.UnsupportedProvider => BadRequest(authResult.Error),
                _ => Problem(authResult.Error)
            };
        }

        var userResult = await _externalUserService.EnsureUserAsync(authResult.LoginInfo!);
        if (!userResult.Succeeded)
        {
            return userResult.ErrorType switch
            {
                ExternalUserError.Unauthorized => Unauthorized(),
                ExternalUserError.BadRequest => BadRequest(userResult.Error),
                _ => Problem(userResult.Error)
            };
        }

        var pair = await _authenticationService.GenerateTokenAsync(userResult.UserId!);
        SetRefreshTokenCookie(pair.RefreshToken, pair.RefreshTokenExpiryTime);
        return Ok(new TokenResponse(pair.Token));
    }
}

