using System;
using System.Threading;
using Avancira.Application.Persistence;
using Avancira.Application.StudentProfiles.Dtos;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Students;
using Mapster;

namespace Avancira.Application.StudentProfiles;

public class StudentProfileService : IStudentProfileService
{
    private readonly IRepository<StudentProfile> _studentProfileRepository;

    public StudentProfileService(IRepository<StudentProfile> studentProfileRepository)
    {
        _studentProfileRepository = studentProfileRepository;
    }

    public async Task<StudentProfileDto> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var profile = await _studentProfileRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Student profile for user '{userId}' not found.");

        return profile.Adapt<StudentProfileDto>();
    }

    public async Task<StudentProfileDto> UpdateAsync(StudentProfileUpdateDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await _studentProfileRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Student profile for user '{request.UserId}' not found.");

        profile.UpdateLearningPreferences(
            request.LearningGoal,
            request.CurrentEducationLevel,
            request.School,
            request.Major,
            request.PreferredLearningStyle);

        await _studentProfileRepository.UpdateAsync(profile, cancellationToken);

        return profile.Adapt<StudentProfileDto>();
    }
}
