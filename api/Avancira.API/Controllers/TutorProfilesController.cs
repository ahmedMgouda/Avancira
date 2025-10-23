using System.Threading;
using Avancira.Application.TutorProfiles;
using Avancira.Application.TutorProfiles.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/tutors/profile")]
public class TutorProfilesController : BaseApiController
{
    private readonly ITutorProfileService _tutorProfileService;

    public TutorProfilesController(ITutorProfileService tutorProfileService)
    {
        _tutorProfileService = tutorProfileService;
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(TutorProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTutorProfile(string userId, CancellationToken cancellationToken)
    {
        var profile = await _tutorProfileService.GetByUserIdAsync(userId, cancellationToken);
        return Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType(typeof(TutorProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTutorProfile([FromBody] TutorProfileUpdateDto request, CancellationToken cancellationToken)
    {
        var profile = await _tutorProfileService.UpdateAsync(request, cancellationToken);
        return Ok(profile);
    }

    [HttpPost("verify")]
    [ProducesResponseType(typeof(TutorProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyTutorProfile([FromBody] TutorProfileVerificationDto request, CancellationToken cancellationToken)
    {
        var profile = await _tutorProfileService.VerifyAsync(request, cancellationToken);
        return Ok(profile);
    }
}
