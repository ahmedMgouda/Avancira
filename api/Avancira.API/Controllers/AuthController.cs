using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Infrastructure.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Avancira.API.Controllers;

[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly ITokenService _tokenService;

    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "GenerateToken")]
    public async Task<ActionResult<TokenResponse>> GenerateToken([FromBody] TokenGenerationDto request, CancellationToken cancellationToken)
    {
        string deviceId = HttpContext.GetDeviceIdentifier();
        string ip = HttpContext.GetIpAddress();
        string userAgent = HttpContext.GetUserAgent();
        string operatingSystem = HttpContext.GetOperatingSystem();

        var result = await _tokenService.GenerateTokenAsync(request, deviceId, ip, userAgent, operatingSystem, cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiryTime);

        return Ok(new TokenResponse(result.Token));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "RefreshToken")]
    public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenDto? request, CancellationToken cancellationToken)
    {
        string deviceId = HttpContext.GetDeviceIdentifier();
        string ip = HttpContext.GetIpAddress();
        string userAgent = HttpContext.GetUserAgent();
        string operatingSystem = HttpContext.GetOperatingSystem();

        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized();
        }

        var result = await _tokenService.RefreshTokenAsync(request?.Token, refreshToken, deviceId, ip, userAgent, operatingSystem, cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiryTime);

        return Ok(new TokenResponse(result.Token));
    }

    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "RevokeToken")]
    public async Task<IActionResult> RevokeToken(CancellationToken cancellationToken)
    {
        string deviceId = HttpContext.GetDeviceIdentifier();
        string userId = GetUserId();
        var refreshToken = Request.Cookies["refreshToken"];

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await _tokenService.RevokeTokenAsync(refreshToken, userId, deviceId, cancellationToken);
        }

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            Path = "/api/auth"
        });
        return Ok();
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expires)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expires,
            Path = "/api/auth"
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}
