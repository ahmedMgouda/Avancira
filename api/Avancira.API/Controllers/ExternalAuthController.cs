using Avancira.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[ApiController]
[Route("api/auth")]
public class ExternalAuthController : BaseApiController
{
    [HttpGet("external-login")]
    [AllowAnonymous]
    public IActionResult ExternalLogin([FromQuery] string provider)
    {
        if (string.IsNullOrWhiteSpace(provider) ||
            !Enum.TryParse<SocialProvider>(provider, true, out var parsed))
        {
            return BadRequest();
        }

        var normalized = parsed.ToString().ToLowerInvariant();

        return Redirect($"/api/auth/external/{normalized}");
    }
}
