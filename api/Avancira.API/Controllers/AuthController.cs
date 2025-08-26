using Avancira.Application.Identity;
using Avancira.Application.Identity.Tokens.Dtos;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly IAuthenticationService _authenticationService;

    public AuthController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
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
}

