using Avancira.Application.Identity;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Users.Dtos;
using System;
using System.IdentityModel.Tokens.Jwt;
using Avancira.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ISessionService _sessionService;

    public AuthController(
        IAuthenticationService authenticationService,
        ISessionService sessionService)
    {
        _authenticationService = authenticationService;
        _sessionService = sessionService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequestDto request)
    {
        var pair = await _authenticationService.PasswordSignInAsync(request.Email, request.Password);
        if (pair is null)
        {
            return Unauthorized();
        }

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

        var hash = TokenUtilities.HashToken(refreshToken);
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

        await _sessionService.RotateRefreshTokenAsync(info.Value.RefreshTokenId, TokenUtilities.HashToken(pair.RefreshToken), pair.RefreshTokenExpiryTime);
        SetRefreshTokenCookie(pair.RefreshToken, pair.RefreshTokenExpiryTime);
        return Ok(new TokenResponse(pair.Token));
    }

}

