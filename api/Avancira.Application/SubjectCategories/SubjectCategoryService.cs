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
    private const int SortOrderInterval = 10; // Spacing between items: 10, 20, 30...

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
        // ============================================================
        // AUTO-ASSIGN SORTORDER BASED ON INSERT POSITION
        // ============================================================
        int newSortOrder = await DetermineInsertPosition(request);

        var entity = SubjectCategory.Create(
            request.Name,
            request.Description,
            request.IsActive,
            request.IsVisible,
            request.IsFeatured,
            newSortOrder); // Auto-assigned!

        await _repository.AddAsync(entity);
        return entity.Adapt<SubjectCategoryDto>();
    }

    public async Task<SubjectCategoryDto> UpdateAsync(SubjectCategoryUpdateDto request)
    {
        var entity = await _repository.GetByIdAsync(request.Id)
            ?? throw new AvanciraNotFoundException($"Subject category '{request.Id}' not found.");

        // Update everything EXCEPT sortOrder
        // SortOrder is only changed via Reorder/Move endpoints
        entity.Update(
            request.Name,
            request.Description,
            request.IsActive,
            request.IsVisible,
            request.IsFeatured,
            entity.SortOrder); // Keep existing sortOrder!

        await _repository.UpdateAsync(entity);
        return entity.Adapt<SubjectCategoryDto>();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new AvanciraNotFoundException($"Subject category '{id}' not found.");

        await _repository.DeleteAsync(entity);
    }

    public async Task ReorderAsync(int[] categoryIds)
    {
        var spec = new CategoryByIdsSpec(categoryIds);
        var items = await _repository.ListAsync(spec);

        // Reorder based on the provided array order
        items = items.OrderBy(c => Array.IndexOf(categoryIds, c.Id)).ToList();

        // ============================================================
        // FIX: Assign 10, 20, 30... instead of 1, 2, 3
        // ============================================================
        for (int i = 0; i < items.Count; i++)
        {
            items[i].UpdateSortOrder((i + 1) * SortOrderInterval); // 10, 20, 30, 40...
            await _repository.UpdateAsync(items[i]);
        }
    }

    public async Task MoveToPositionAsync(int id, int targetSortOrder)
    {
        var item = await _repository.GetByIdAsync(id)
            ?? throw new AvanciraNotFoundException($"Category '{id}' not found.");

        var current = item.SortOrder;

        // Check if target position is already taken
        var conflictSpec = new CategoryBySortOrderSpec(targetSortOrder);
        var conflict = await _repository.FirstOrDefaultAsync(conflictSpec);

        if (conflict != null && conflict.Id != id)
        {
            // SWAP: Exchange sortOrder values
            conflict.UpdateSortOrder(current);
            item.UpdateSortOrder(targetSortOrder);

            await _repository.UpdateAsync(conflict);
            await _repository.UpdateAsync(item);
        }
        else
        {
            // Target is free, just move
            item.UpdateSortOrder(targetSortOrder);
            await _repository.UpdateAsync(item);
        }
    }

    // ============================================================
    // PRIVATE HELPER METHODS - AUTO-ASSIGNMENT LOGIC
    // ============================================================

    /// <summary>
    /// Determines the sortOrder for a new category based on the requested insert position.
    /// </summary>
    private async Task<int> DetermineInsertPosition(SubjectCategoryCreateDto request)
    {
        switch (request.InsertPosition?.ToLower())
        {
            case "start":
                // Insert at the beginning
                var minSpec = new SortOrderRangeSpec(ascending: true, take: 1);
                var firstItem = await _repository.FirstOrDefaultAsync(minSpec);

                if (firstItem == null)
                {
                    return SortOrderInterval; // First item ever
                }

                return Math.Max(SortOrderInterval, firstItem.SortOrder - SortOrderInterval);

            case "custom" when request.CustomPosition.HasValue:
                // User specified exact position
                var targetPosition = request.CustomPosition.Value;

                // Check if position is taken
                var conflictSpec = new CategoryBySortOrderSpec(targetPosition);
                var existing = await _repository.FirstOrDefaultAsync(conflictSpec);

                if (existing != null)
                {
                    // Find next available position
                    return await FindNextAvailablePosition(targetPosition);
                }

                return targetPosition;

            case "end":
            default:
                // Insert at the end (default)
                var maxSpec = new SortOrderRangeSpec(ascending: false, take: 1);
                var lastItem = await _repository.FirstOrDefaultAsync(maxSpec);

                if (lastItem == null)
                {
                    return SortOrderInterval; // First item ever
                }

                return lastItem.SortOrder + SortOrderInterval;
        }
    }

    /// <summary>
    /// Finds the next available sortOrder position starting from the desired position.
    /// </summary>
    private async Task<int> FindNextAvailablePosition(int desiredPosition)
    {
        int position = desiredPosition;

        // Keep incrementing until we find an available position
        while (true)
        {
            var spec = new CategoryBySortOrderSpec(position);
            var existing = await _repository.FirstOrDefaultAsync(spec);

            if (existing == null)
            {
                return position; // Found available position
            }

            position += SortOrderInterval;
        }
    }
}
