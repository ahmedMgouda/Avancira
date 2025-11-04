using Avancira.Application.Common.Models;
using Avancira.Application.Persistence;
using Avancira.Application.SubjectCategories.Dtos;
using Avancira.Application.SubjectCategories.Specifications;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Subjects;
using Mapster;

namespace Avancira.Application.SubjectCategories;

public sealed class SubjectCategoryService : ISubjectCategoryService
{
    private readonly IRepository<SubjectCategory> _repository;

    public SubjectCategoryService(IRepository<SubjectCategory> repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedResult<SubjectCategoryDto>> GetAllAsync(SubjectCategoryFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var spec = new SubjectCategoryFilterSpec(filter);

        var totalCount = await _repository.CountAsync(spec);
        var items = await _repository.ListAsync(spec);

        return new PaginatedResult<SubjectCategoryDto>
        {
            Items = items.Adapt<IReadOnlyList<SubjectCategoryDto>>(),
            TotalCount = totalCount,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        };
    }

    public async Task<SubjectCategoryDto> GetByIdAsync(int id)
    {
        var category = await _repository.GetByIdAsync(id)
            ?? throw new AvanciraNotFoundException($"Subject category '{id}' not found.");

        return category.Adapt<SubjectCategoryDto>();
    }

    public async Task<SubjectCategoryDto> CreateAsync(SubjectCategoryCreateDto request)
    {
        var entity = SubjectCategory.Create(
            request.Name,
            request.Description,
            request.IsActive,
            request.IsVisible,
            request.IsFeatured,
            request.SortOrder);

        await _repository.AddAsync(entity);
        return entity.Adapt<SubjectCategoryDto>();
    }

    public async Task<SubjectCategoryDto> UpdateAsync(SubjectCategoryUpdateDto request)
    {
        var entity = await _repository.GetByIdAsync(request.Id)
            ?? throw new AvanciraNotFoundException($"Subject category '{request.Id}' not found.");

        entity.Update(
            request.Name,
            request.Description,
            request.IsActive,
            request.IsVisible,
            request.IsFeatured,
            request.SortOrder);

        await _repository.UpdateAsync(entity);
        return entity.Adapt<SubjectCategoryDto>();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new AvanciraNotFoundException($"Subject category '{id}' not found.");

        await _repository.DeleteAsync(entity);
    }
}
