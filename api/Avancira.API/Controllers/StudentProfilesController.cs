using System.Threading;
using Avancira.Application.StudentProfiles;
using Avancira.Application.StudentProfiles.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/students/profile")]
public class StudentProfilesController : BaseApiController
{
    private readonly IStudentProfileService _studentProfileService;

    public StudentProfilesController(IStudentProfileService studentProfileService)
    {
        _studentProfileService = studentProfileService;
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(StudentProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentProfile(string userId, CancellationToken cancellationToken)
    {
        var profile = await _studentProfileService.GetByUserIdAsync(userId, cancellationToken);
        return Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType(typeof(StudentProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStudentProfile([FromBody] StudentProfileUpdateDto request, CancellationToken cancellationToken)
    {
        var profile = await _studentProfileService.UpdateAsync(request, cancellationToken);
        return Ok(profile);
    }
}
