using System;
using Avancira.Application.Persistence;
using Avancira.Application.UserPreferences.Dtos;
using Avancira.Application.UserPreferences.Specifications;
using Avancira.Domain.Users;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Avancira.Application.UserPreferences;

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
                "Created default user preference for {UserId} with profile {ActiveProfile}",
                userId,
                preference.ActiveProfile);
        }

        return preference.Adapt<UserPreferenceDto>();
    }

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
                userId,
                normalizedProfile);
        }
        else
        {
            if (!string.Equals(preference.ActiveProfile, normalizedProfile, StringComparison.Ordinal))
            {
                preference.SwitchProfile(normalizedProfile);
                await _repository.UpdateAsync(preference, cancellationToken);
                _logger.LogInformation(
                    "Updated active profile for {UserId} to {ActiveProfile}",
                    userId,
                    normalizedProfile);
            }
            else
            {
                _logger.LogDebug(
                    "Active profile for {UserId} already {ActiveProfile}",
                    userId,
                    normalizedProfile);
            }
        }

        return preference.Adapt<UserPreferenceDto>();
    }

    private static string NormalizeProfile(string profile)
    {
        var trimmed = profile.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Profile cannot be empty after trimming", nameof(profile));
        }

        return trimmed.ToLowerInvariant();
    }
}
