using System.Collections.Generic;
using System.Linq;

namespace Avancira.Application.Paging;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Results { get; }
    public int TotalResults { get; }
    public int Page { get; }
    public int PageSize { get; }

    public PagedResult(IEnumerable<T> results, int totalResults, int page, int pageSize)
    {
        Results = results.ToList();
        TotalResults = totalResults;
        Page = page;
        PageSize = pageSize;
    }
}
