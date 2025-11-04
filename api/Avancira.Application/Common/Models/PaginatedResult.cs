using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Common.Models
{
    public sealed class PaginatedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int TotalCount { get; init; }
        public int PageIndex { get; init; }
        public int PageSize { get; init; }
        public int TotalPages { get; init; }
        public bool HasPreviousPage => PageIndex > 0;
        public bool HasNextPage => PageIndex < TotalPages - 1;
        public static PaginatedResult<T> Create(
            IEnumerable<T> source,
            int totalCount,
            int pageIndex,
            int pageSize)
        {
            var list = source?.ToList() ?? new List<T>();
            return new PaginatedResult<T>
            {
                Items = list,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
    }
}
