using System;

namespace Avancira.Application.UserPreferences.Dtos;

public class UserPreferenceDto
{
    public string Id { get; set; } = default!;

    public string UserId { get; set; } = default!;

    public string ActiveProfile { get; set; } = default!;

    public DateTime UpdatedOnUtc { get; set; }
}
