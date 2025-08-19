using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;

namespace Avancira.API.Controllers;

[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly ITokenService _tokenService;
    private readonly IClientInfoService _clientInfoService;

    public AuthController(ITokenService tokenService, IClientInfoService clientInfoService)
    {
        _tokenService = tokenService;
        _clientInfoService = clientInfoService;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "GenerateToken")]
    public async Task<ActionResult<TokenResponse>> GenerateToken([FromBody] TokenGenerationDto request, CancellationToken cancellationToken)
    {
        var clientInfo = await _clientInfoService.GetClientInfoAsync();

        var result = await _tokenService.GenerateTokenAsync(request, clientInfo, cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken, request.RememberMe ? result.RefreshTokenExpiryTime : null);

        return Ok(new TokenResponse(result.Token));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "RefreshToken")]
    public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenDto? request, CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized();
        }

        var clientInfo = await _clientInfoService.GetClientInfoAsync();

        var result = await _tokenService.RefreshTokenAsync(request?.Token, refreshToken, clientInfo, cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiryTime);

        return Ok(new TokenResponse(result.Token));
    }

    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "RevokeToken")]
    public async Task<IActionResult> RevokeToken(CancellationToken cancellationToken)
    {
        var clientInfo = await _clientInfoService.GetClientInfoAsync();
        string userId = GetUserId();
        var refreshToken = Request.Cookies["refreshToken"];

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await _tokenService.RevokeTokenAsync(refreshToken, userId, clientInfo, cancellationToken);
        }

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            Path = "/api/auth"
        });
        return Ok();
    }

    [HttpGet("sessions")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<SessionDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "GetSessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        string userId = GetUserId();
        var sessions = await _tokenService.GetSessionsAsync(userId, cancellationToken);
        return Ok(sessions);
    }

    [HttpDelete("sessions/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "RevokeSession")]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken cancellationToken)
    {
        string userId = GetUserId();
        await _tokenService.RevokeSessionAsync(id, userId, cancellationToken);
        return Ok();
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime? expires)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth"
        };

        if (expires.HasValue)
        {
            cookieOptions.Expires = expires.Value;
        }

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}
