using Avancira.Application.TutorProfiles;
using Avancira.Application.TutorProfiles.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/tutors/profile")]
public class TutorProfilesController : BaseApiController
{
    private readonly ITutorProfileService _service;

    public TutorProfilesController(ITutorProfileService service)
    {
        _service = service;
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(TutorProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string userId, CancellationToken cancellationToken)
    {
        var profile = await _service.GetByUserIdAsync(userId, cancellationToken);
        return Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType(typeof(TutorProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromBody] TutorProfileUpdateDto request, CancellationToken cancellationToken)
    {
        var profile = await _service.UpdateAsync(request, cancellationToken);
        return Ok(profile);
    }

    [HttpPost("verify")]
    [ProducesResponseType(typeof(TutorProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Verify([FromBody] TutorProfileVerificationDto request, CancellationToken cancellationToken)
    {
        var profile = await _service.VerifyAsync(request, cancellationToken);
        return Ok(profile);
    }
}
