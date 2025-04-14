using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Infrastructure.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Avancira.API.Controllers;

[Route("api/[controller]")]
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
        string ip = HttpContext.GetIpAddress();

        var result = await _tokenService.GenerateTokenAsync(request, ip, cancellationToken);

        if (result != null)
        {
            return Ok(result);
        }

        return BadRequest("Token generation failed");
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "RefreshToken")]
    public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenDto request, CancellationToken cancellationToken)
    {
        string ip = HttpContext.GetIpAddress();

        var result = await _tokenService.RefreshTokenAsync(request, ip, cancellationToken);

        return Ok(result);
    }
}
