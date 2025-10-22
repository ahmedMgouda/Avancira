using System;
using System.Linq;
using Avancira.Application.Persistence;
using Avancira.Application.Subjects.Dtos;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Subjects;
using Mapster;

namespace Avancira.Application.Subjects;

public class SubjectService : ISubjectService
{
    private readonly IRepository<Subject> _subjectRepository;
    private readonly IReadRepository<SubjectCategory> _subjectCategoryReadRepository;

    public SubjectService(
        IRepository<Subject> subjectRepository,
        IReadRepository<SubjectCategory> subjectCategoryReadRepository)
    {
        _subjectRepository = subjectRepository;
        _subjectCategoryReadRepository = subjectCategoryReadRepository;
    }

    public async Task<SubjectDto> GetByIdAsync(int id)
    {
        var subject = await _subjectRepository.GetByIdAsync(id)
            ?? throw new AvanciraNotFoundException($"Subject with ID '{id}' not found.");

        return subject.Adapt<SubjectDto>();
    }

    public async Task<IEnumerable<SubjectDto>> GetAllAsync(int? categoryId = null)
    {
        var subjects = await _subjectRepository.ListAsync();

        if (categoryId.HasValue)
        {
            subjects = subjects
                .Where(subject => subject.CategoryId == categoryId.Value)
                .ToList();
        }

        return subjects
            .OrderBy(subject => subject.SortOrder)
            .ThenBy(subject => subject.Name)
            .Adapt<IEnumerable<SubjectDto>>();
    }

    public async Task<SubjectDto> CreateAsync(SubjectCreateDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureCategoryExistsAsync(request.CategoryId);

        var subject = Subject.Create(
            request.Name,
            request.Description,
            request.IconUrl,
            request.IsActive,
            request.IsVisible,
            request.IsFeatured,
            request.SortOrder,
            request.CategoryId);

        await _subjectRepository.AddAsync(subject);

        return subject.Adapt<SubjectDto>();
    }

    public async Task<SubjectDto> UpdateAsync(SubjectUpdateDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subject = await _subjectRepository.GetByIdAsync(request.Id)
            ?? throw new AvanciraNotFoundException($"Subject with ID '{request.Id}' not found. Update operation aborted.");

        await EnsureCategoryExistsAsync(request.CategoryId);

        subject.Update(
            request.Name,
            request.Description,
            request.IconUrl,
            request.IsActive,
            request.IsVisible,
            request.IsFeatured,
            request.SortOrder,
            request.CategoryId);

        await _subjectRepository.UpdateAsync(subject);

        return subject.Adapt<SubjectDto>();
    }

    public async Task DeleteAsync(int id)
    {
        var subject = await _subjectRepository.GetByIdAsync(id)
            ?? throw new AvanciraNotFoundException($"Subject with ID '{id}' not found. Deletion operation aborted.");

        await _subjectRepository.DeleteAsync(subject);
    }

    private async Task EnsureCategoryExistsAsync(int categoryId)
    {
        var categoryExists = await _subjectCategoryReadRepository.GetByIdAsync(categoryId) is not null;

        if (!categoryExists)
        {
            throw new AvanciraNotFoundException($"Subject category with ID '{categoryId}' not found.");
        }
    }
}
