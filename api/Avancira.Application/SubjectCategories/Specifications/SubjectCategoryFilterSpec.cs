using Ardalis.Specification;
using Avancira.Application.SubjectCategories.Dtos;
using Avancira.Domain.Subjects;

namespace Avancira.Application.SubjectCategories.Specifications;

/// <summary>
/// Specification for filtering, searching, and sorting SubjectCategory entities.
/// Works with any IRepository implementation (EF, Dapper, etc.).
/// </summary>
public sealed class SubjectCategoryFilterSpec : Specification<SubjectCategory>
{
    public SubjectCategoryFilterSpec(SubjectCategoryFilter filter)
    {
        Query.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            Query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.Description ?? string.Empty).ToLower().Contains(term));
        }

        if (filter.IsActive.HasValue)
            Query.Where(c => c.IsActive == filter.IsActive.Value);

        if (filter.IsVisible.HasValue)
            Query.Where(c => c.IsVisible == filter.IsVisible.Value);

        if (filter.IsFeatured.HasValue)
            Query.Where(c => c.IsFeatured == filter.IsFeatured.Value);

        var sortBy = filter.SortBy?.ToLowerInvariant() ?? "id";

        switch (sortBy)
        {
            case "name":
                if (filter.IsDescending)
                    Query.OrderByDescending(c => c.Name);
                else
                    Query.OrderBy(c => c.Name);
                break;

            case "sortorder":
                if (filter.IsDescending)
                    Query.OrderByDescending(c => c.SortOrder);
                else
                    Query.OrderBy(c => c.SortOrder);
                break;

            default:
                if (filter.IsDescending)
                    Query.OrderByDescending(c => c.Id);
                else
                    Query.OrderBy(c => c.Id);
                break;
        }

        if (filter.PageSize > 0)
        {
            var skip = filter.PageIndex * filter.PageSize;
            Query.Skip(skip).Take(filter.PageSize);
        }
    }
}
