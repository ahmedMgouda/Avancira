using Ardalis.Specification;
using Avancira.Domain.Subjects;

namespace Avancira.Application.SubjectCategories.Specifications;

/// <summary>
/// Specification for querying categories by sortOrder range.
/// Used to find first/last items for auto-assignment logic.
/// </summary>
public sealed class SortOrderRangeSpec : Specification<SubjectCategory>
{
    /// <summary>
    /// Creates a specification that orders categories by sortOrder.
    /// </summary>
    /// <param name="ascending">True for ascending order (first item), false for descending (last item)</param>
    /// <param name="take">Optional limit on number of results</param>
    public SortOrderRangeSpec(bool ascending = true, int? take = null)
    {
        if (ascending)
        {
            Query.OrderBy(c => c.SortOrder);
        }
        else
        {
            Query.OrderByDescending(c => c.SortOrder);
        }

        if (take.HasValue)
        {
            Query.Take(take.Value);
        }
    }
}
