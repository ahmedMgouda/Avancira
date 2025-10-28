using System;
using System.Linq;
using Avancira.Application.Persistence;
using Avancira.Application.SubjectCategories.Dtos;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Subjects;
using Mapster;

namespace Avancira.Application.SubjectCategories;

public class SubjectCategoryService : ISubjectCategoryService
{
    private readonly IRepository<SubjectCategory> _subjectCategoryRepository;

    public SubjectCategoryService(IRepository<SubjectCategory> subjectCategoryRepository)
    {
        _subjectCategoryRepository = subjectCategoryRepository;
    }

    public async Task<SubjectCategoryDto> GetByIdAsync(int id)
    {
        var subjectCategory = await _subjectCategoryRepository.GetByIdAsync(id)
            ?? throw new AvanciraNotFoundException($"Subject category with ID '{id}' not found.");

        return subjectCategory.Adapt<SubjectCategoryDto>();
    }

    public async Task<IEnumerable<SubjectCategoryDto>> GetAllAsync()
    {
        var subjectCategories = await _subjectCategoryRepository.ListAsync();

        return subjectCategories
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Adapt<IEnumerable<SubjectCategoryDto>>();
    }

    public async Task<SubjectCategoryDto> CreateAsync(SubjectCategoryCreateDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subjectCategory = SubjectCategory.Create(
            request.Name,
            request.Description,
            request.IsActive,
            request.IsVisible,
            request.IsFeatured,
            request.SortOrder);

        await _subjectCategoryRepository.AddAsync(subjectCategory);

        return subjectCategory.Adapt<SubjectCategoryDto>();
    }

    public async Task<SubjectCategoryDto> UpdateAsync(SubjectCategoryUpdateDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subjectCategory = await _subjectCategoryRepository.GetByIdAsync(request.Id)
            ?? throw new AvanciraNotFoundException($"Subject category with ID '{request.Id}' not found. Update operation aborted.");

        subjectCategory.Update(
            request.Name,
            request.Description,
            request.IsActive,
            request.IsVisible,
            request.IsFeatured,
            request.SortOrder);

        await _subjectCategoryRepository.UpdateAsync(subjectCategory);

        return subjectCategory.Adapt<SubjectCategoryDto>();
    }

    public async Task DeleteAsync(int id)
    {
        var subjectCategory = await _subjectCategoryRepository.GetByIdAsync(id)
            ?? throw new AvanciraNotFoundException($"Subject category with ID '{id}' not found. Deletion operation aborted.");

        await _subjectCategoryRepository.DeleteAsync(subjectCategory);
    }
}
