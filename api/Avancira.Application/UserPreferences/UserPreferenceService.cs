using System;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.Persistence;
using Avancira.Application.UserPreferences.Dtos;
using Avancira.Application.UserPreferences.Specifications;
using Avancira.Domain.Users;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Avancira.Application.UserPreferences;

/// <summary>
/// Handles user preference persistence and active profile switching.
/// </summary>
public sealed class UserPreferenceService : IUserPreferenceService
{
    private readonly IRepository<UserPreference> _repository;
    private readonly ILogger<UserPreferenceService> _logger;

    public UserPreferenceService(
        IRepository<UserPreference> repository,
        ILogger<UserPreferenceService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Gets existing preference or creates a default one for the user.
    /// </summary>
    public async Task<UserPreferenceDto> GetOrCreateAsync(
        string userId,
        string defaultProfile = "student",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultProfile);

        var normalizedProfile = NormalizeProfile(defaultProfile);

        var preference = await _repository.FirstOrDefaultAsync(
            new UserPreferenceByUserSpec(userId),
            cancellationToken);

        if (preference is null)
        {
            preference = UserPreference.Create(userId, normalizedProfile);
            await _repository.AddAsync(preference, cancellationToken);

            _logger.LogInformation(
                "Created new user preference for {UserId} with active profile {ActiveProfile}",
                userId, preference.ActiveProfile);
        }

        return preference.Adapt<UserPreferenceDto>();
    }

    /// <summary>
    /// Updates (or initializes) the active profile for the user.
    /// </summary>
    public async Task<UserPreferenceDto> SetActiveProfileAsync(
        string userId,
        string newProfile,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(newProfile);

        var normalizedProfile = NormalizeProfile(newProfile);

        var preference = await _repository.FirstOrDefaultAsync(
            new UserPreferenceByUserSpec(userId),
            cancellationToken);

        if (preference is null)
        {
            preference = UserPreference.Create(userId, normalizedProfile);
            await _repository.AddAsync(preference, cancellationToken);

            _logger.LogInformation(
                "Initialized user preference for {UserId} with profile {ActiveProfile}",
                userId, normalizedProfile);
        }
        else if (!string.Equals(preference.ActiveProfile, normalizedProfile, StringComparison.OrdinalIgnoreCase))
        {
            preference.SwitchProfile(normalizedProfile);
            // No explicit update needed if repository tracks entities
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Switched active profile for {UserId} to {ActiveProfile}",
                userId, normalizedProfile);
        }
        else
        {
            _logger.LogTrace(
                "User {UserId} already in active profile {ActiveProfile}",
                userId, normalizedProfile);
        }

        return preference.Adapt<UserPreferenceDto>();
    }

    /// <summary>
    /// Normalizes profile names to lower-case (student, tutor, admin).
    /// </summary>
    private static string NormalizeProfile(string profile)
    {
        var trimmed = profile.Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Profile cannot be empty.", nameof(profile));

        return trimmed.ToLowerInvariant();
    }
}
