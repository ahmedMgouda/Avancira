using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Infrastructure.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Avancira.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [SwaggerOperation(
        OperationId = "GenerateToken",
        Summary = "Generates an authentication token",
        Description = "Generates an authentication token based on the provided credentials. This operation is public and does not require user authentication."
    )]
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
    [SwaggerOperation(
        OperationId = "RefreshToken",
        Summary = "Refreshes an existing authentication token",
        Description = "Refreshes an existing authentication token. This operation is public and does not require user authentication."
    )]
    public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenDto request, CancellationToken cancellationToken)
    {
        string ip = HttpContext.GetIpAddress();

        var result = await _tokenService.RefreshTokenAsync(request, ip, cancellationToken);

        return Ok(result);
    }
}
