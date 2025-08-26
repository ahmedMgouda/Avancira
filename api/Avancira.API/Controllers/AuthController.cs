using Avancira.Application.Identity;
using Avancira.Application.Auth;
using Avancira.Application.Auth.Dtos;
using Avancira.Application.Identity.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IExternalAuthService _externalAuthService;
    private readonly IExternalUserService _externalUserService;
    private readonly ISessionService _sessionService;

    public AuthController(
        IAuthenticationService authenticationService,
        IExternalAuthService externalAuthService,
        IExternalUserService externalUserService,
        ISessionService sessionService)
    {
        _authenticationService = authenticationService;
        _externalAuthService = externalAuthService;
        _externalUserService = externalUserService;
        _sessionService = sessionService;
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

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Refresh()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized();
        }

        var hash = HashToken(refreshToken);
        var info = await _sessionService.GetRefreshTokenInfoAsync(hash);
        if (info is null)
        {
            return Unauthorized();
        }

        var pair = await _authenticationService.RefreshTokenAsync(refreshToken);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(pair.Token);
        if (jwtToken.Subject != info.Value.UserId)
        {
            return Unauthorized();
        }

        await _sessionService.RotateRefreshTokenAsync(info.Value.RefreshTokenId, HashToken(pair.RefreshToken), pair.RefreshTokenExpiryTime);
        SetRefreshTokenCookie(pair.RefreshToken, pair.RefreshTokenExpiryTime);
        return Ok(new TokenResponse(pair.Token));
    }

    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}

