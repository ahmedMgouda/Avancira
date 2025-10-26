using Avancira.Application.UserPreferences.Dtos;

namespace Avancira.Application.UserPreferences;

public interface IUserPreferenceService
{
    Task<UserPreferenceDto> GetOrCreateAsync(
        string userId,
        string defaultProfile = "student",
        CancellationToken cancellationToken = default);

    Task<UserPreferenceDto> SetActiveProfileAsync(
        string userId,
        string newProfile,
        CancellationToken cancellationToken = default);
}
