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
        return Redirect($"/connect/authorize?provider={provider}");
    }
}
