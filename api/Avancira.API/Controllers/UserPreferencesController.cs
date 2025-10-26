using System.ComponentModel.DataAnnotations;
using Avancira.Application.UserPreferences;
using Avancira.Application.UserPreferences.Dtos;
using Microsoft.AspNetCore.Mvc;
using Avancira.Shared.Authorization;

namespace Avancira.API.Controllers;

[Route("api/user-preferences")]
public sealed class UserPreferencesController : BaseApiController
{
    private readonly IUserPreferenceService _userPreferenceService;

    public UserPreferencesController(IUserPreferenceService userPreferenceService)
    {
        _userPreferenceService = userPreferenceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserPreferenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var preference = await _userPreferenceService.GetOrCreateAsync(userId, cancellationToken: cancellationToken);
        return Ok(preference);
    }

    [HttpPut]
    [ProducesResponseType(typeof(UserPreferenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        [FromBody] UpdateActiveProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var preference = await _userPreferenceService.SetActiveProfileAsync(userId, request.ActiveProfile, cancellationToken);
        return Ok(preference);
    }
}

public sealed class UpdateActiveProfileRequest
{
    [Required]
    public string ActiveProfile { get; init; } = default!;
}
