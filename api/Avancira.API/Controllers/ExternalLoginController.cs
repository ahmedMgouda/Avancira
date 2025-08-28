using System;
using Avancira.Application.Auth;
using Avancira.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[ApiController]
[Route("api/auth/external")]
public class ExternalLoginController : BaseApiController
{
    [HttpGet("{provider}")]
    [AllowAnonymous]
    public IActionResult ExternalLogin([FromRoute] string provider)
    {
        if (string.IsNullOrWhiteSpace(provider) ||
            !Enum.TryParse<SocialProvider>(provider, true, out var parsed))
        {
            return BadRequest();
        }

        var normalized = parsed.ToString().ToLowerInvariant();
        var callback = "/api/auth/external/callback";
        var encodedCallback = Uri.EscapeDataString(callback);
        var url = $"{AuthConstants.Endpoints.Authorize}?{AuthConstants.Parameters.Provider}={normalized}&{AuthConstants.Parameters.RedirectUri}={encodedCallback}";
        return Redirect(url);
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public IActionResult Callback()
    {
        return Ok();
    }
}
