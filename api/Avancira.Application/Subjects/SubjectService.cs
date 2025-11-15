using Avancira.Application.Persistence;
using Avancira.Application.Storage;
using Avancira.Application.Subjects.Dtos;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Subjects;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Avancira.Application.Subjects;

public class SubjectService : ISubjectService
{
    private readonly IRepository<Subject> _subjectRepository;
    private readonly IReadRepository<SubjectCategory> _categoryRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<SubjectService> _logger;

    public SubjectService(
        IRepository<Subject> subjectRepository,
        IReadRepository<SubjectCategory> categoryRepository,
        IFileStorageService fileStorage,
        ILogger<SubjectService> logger)
    {
        _subjectRepository = subjectRepository;
        _categoryRepository = categoryRepository;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<SubjectDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var subject = await _subjectRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Subject with ID '{id}' not found.");

        return subject.Adapt<SubjectDto>();
    }

    public async Task<IEnumerable<SubjectDto>> GetAllAsync(
        int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var subjects = await _subjectRepository.ListAsync(cancellationToken);

        // Filter by category if specified
        if (categoryId.HasValue)
        {
            subjects = subjects
                .Where(s => s.CategoryId == categoryId.Value)
                .ToList();
        }

        // Sort by SortOrder, then Name
        return subjects
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .Adapt<IEnumerable<SubjectDto>>();
    }

    public async Task<SubjectDto> CreateAsync(
        SubjectCreateDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate category exists
        await ValidateCategoryExistsAsync(request.CategoryId, cancellationToken);

        // Handle icon upload if provided
        string? iconUrl = null;
        if (request.Icon != null)
        {
            using var fileData = await FileData.FromFormFileAsync(request.Icon, cancellationToken);
            iconUrl = await UploadSubjectIconAsync(fileData, request.Name, cancellationToken);
        }

        // Create subject entity
        var subject = Subject.Create(
            name: request.Name,
            description: request.Description,
            iconUrl: iconUrl ?? request.IconUrl,
            isActive: request.IsActive,
            isVisible: request.IsVisible,
            isFeatured: request.IsFeatured,
            sortOrder: request.SortOrder,
            categoryId: request.CategoryId);

        await _subjectRepository.AddAsync(subject, cancellationToken);

        _logger.LogInformation(
            "Subject created: {SubjectName} (ID: {SubjectId})",
            subject.Name, subject.Id);

        return subject.Adapt<SubjectDto>();
    }

    public async Task<SubjectDto> UpdateAsync(
        SubjectUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Fetch existing subject
        var subject = await _subjectRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new AvanciraNotFoundException(
                $"Subject with ID '{request.Id}' not found. Update operation aborted.");

        // Validate category exists
        await ValidateCategoryExistsAsync(request.CategoryId, cancellationToken);

        // Handle icon replacement if new icon provided
        string? iconUrl = subject.IconUrl;
        if (request.Icon != null)
        {
            // Delete old icon if exists
            if (!string.IsNullOrEmpty(subject.IconUrl))
            {
                await DeleteIconSafelyAsync(subject.IconUrl);
            }

            // Upload new icon
            using var fileData = await FileData.FromFormFileAsync(request.Icon, cancellationToken);
            iconUrl = await UploadSubjectIconAsync(fileData, request.Name, cancellationToken);
        }
        else if (request.IconUrl != null)
        {
            // Use provided URL (e.g., from external source or kept unchanged)
            iconUrl = request.IconUrl;
        }

        // Update subject entity
        subject.Update(
            name: request.Name,
            description: request.Description,
            iconUrl: iconUrl,
            isActive: request.IsActive,
            isVisible: request.IsVisible,
            isFeatured: request.IsFeatured,
            sortOrder: request.SortOrder,
            categoryId: request.CategoryId);

        await _subjectRepository.UpdateAsync(subject, cancellationToken);

        _logger.LogInformation(
            "Subject updated: {SubjectName} (ID: {SubjectId})",
            subject.Name, subject.Id);

        return subject.Adapt<SubjectDto>();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var subject = await _subjectRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AvanciraNotFoundException(
                $"Subject with ID '{id}' not found. Deletion operation aborted.");

        // Delete associated icon if exists
        if (!string.IsNullOrEmpty(subject.IconUrl))
        {
            await DeleteIconSafelyAsync(subject.IconUrl);
        }

        await _subjectRepository.DeleteAsync(subject, cancellationToken);

        _logger.LogInformation(
            "Subject deleted: {SubjectName} (ID: {SubjectId})",
            subject.Name, id);
    }

    private async Task ValidateCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        var categoryExists = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken) != null;

        if (!categoryExists)
        {
            throw new AvanciraNotFoundException(
                $"Subject category with ID '{categoryId}' not found.");
        }
    }

    private async Task<string> UploadSubjectIconAsync(
        FileData fileData,
        string subjectName,
        CancellationToken cancellationToken)
    {
        try
        {
            var uploadOptions = FileUploadOptions.ForImages("subjects");
            uploadOptions.FileName = $"{SanitizeForFileName(subjectName)}-icon";

            return await _fileStorage.UploadAsync(fileData, uploadOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload icon for subject: {SubjectName}", subjectName);
            throw;
        }
    }

    private async Task DeleteIconSafelyAsync(string iconUrl)
    {
        try
        {
            await _fileStorage.DeleteAsync(iconUrl);
        }
        catch (Exception ex)
        {
            // Log but don't throw - icon deletion failure shouldn't break the main operation
            _logger.LogWarning(ex, "Failed to delete icon: {IconUrl}", iconUrl);
        }
    }

    private static string SanitizeForFileName(string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            input, @"[^a-zA-Z0-9\-]", "_").ToLowerInvariant();
    }
}