using System.ComponentModel.DataAnnotations;
using Avancira.Application.UserPreferences;
using Avancira.Application.UserPreferences.Dtos;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

/// <summary>
/// Manages per-user preference data such as the active profile.
/// </summary>
[Authorize]
[Route("api/user-preferences")]
[ApiController]
public sealed class UserPreferencesController : BaseApiController
{
    private readonly IUserPreferenceService _userPreferenceService;
    private readonly ILogger<UserPreferencesController> _logger;

    public UserPreferencesController(
        IUserPreferenceService userPreferenceService,
        ILogger<UserPreferencesController> logger)
    {
        _userPreferenceService = userPreferenceService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the current user’s preferences or creates them if missing.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserPreferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var preference = await _userPreferenceService.GetOrCreateAsync(userId, cancellationToken: cancellationToken);

        _logger.LogDebug("Retrieved preferences for user {UserId} with profile {Profile}", userId, preference.ActiveProfile);
        return Ok(preference);
    }

    /// <summary>
    /// Updates the current user’s active profile (student, tutor, etc.).
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(UserPreferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAsync(
        [FromBody] UpdateActiveProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var preference = await _userPreferenceService.SetActiveProfileAsync(userId, request.ActiveProfile, cancellationToken);

        _logger.LogInformation("User {UserId} updated active profile to {Profile}", userId, request.ActiveProfile);
        return Ok(preference);
    }
}

/// <summary>
/// Request model for updating the user's active profile.
/// </summary>
public sealed class UpdateActiveProfileRequest
{
    [Required]
    [RegularExpression("^(student|tutor|admin)$", ErrorMessage = "ActiveProfile must be one of: student, tutor, admin.")]
    public string ActiveProfile { get; init; } = default!;
}
