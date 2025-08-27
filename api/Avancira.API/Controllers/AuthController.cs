using Avancira.Application.Identity;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Avancira.Application.Identity.Tokens.Dtos;

namespace Avancira.API.Controllers;

[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IRefreshTokenCookieService _refreshTokenCookieService;

    public AuthController(
        IAuthenticationService authenticationService,
        IRefreshTokenCookieService refreshTokenCookieService)
    {
        _authenticationService = authenticationService;
        _refreshTokenCookieService = refreshTokenCookieService;
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

        _refreshTokenCookieService.SetRefreshTokenCookie(pair.RefreshToken, pair.RefreshTokenExpiryTime);
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

        var pair = await _authenticationService.RefreshTokenAsync(refreshToken);
        _refreshTokenCookieService.SetRefreshTokenCookie(pair.RefreshToken, pair.RefreshTokenExpiryTime);
        return Ok(new TokenResponse(pair.Token));
    }

}

