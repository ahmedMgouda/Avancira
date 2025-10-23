using System.Collections.Generic;
using System.Threading;
using Avancira.Application.LessonMaterials;
using Avancira.Application.LessonMaterials.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/lessons/{lessonId:int}/materials")]
public class LessonMaterialsController : BaseApiController
{
    private readonly ILessonMaterialService _lessonMaterialService;

    public LessonMaterialsController(ILessonMaterialService lessonMaterialService)
    {
        _lessonMaterialService = lessonMaterialService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<LessonMaterialDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLessonMaterials(int lessonId, CancellationToken cancellationToken)
    {
        var materials = await _lessonMaterialService.GetByLessonIdAsync(lessonId, cancellationToken);
        return Ok(materials);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LessonMaterialDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLessonMaterial(int lessonId, [FromBody] LessonMaterialCreateDto request, CancellationToken cancellationToken)
    {
        request.LessonId = lessonId;
        var material = await _lessonMaterialService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetLessonMaterials), new { lessonId = material.LessonId }, material);
    }

    [HttpPost("scan")]
    [ProducesResponseType(typeof(LessonMaterialDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateScanStatus(int lessonId, [FromBody] LessonMaterialScanUpdateDto request, CancellationToken cancellationToken)
    {
        var material = await _lessonMaterialService.UpdateScanStatusAsync(request, cancellationToken);
        if (material.LessonId != lessonId)
        {
            return BadRequest("Material does not belong to specified lesson.");
        }

        return Ok(material);
    }
}
