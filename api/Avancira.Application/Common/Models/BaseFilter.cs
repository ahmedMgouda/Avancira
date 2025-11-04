using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Common.Models
{
    public abstract class BaseFilter
    {
        private int _pageIndex = 0;
        private int _pageSize = 25;

        public string? SearchTerm { get; set; }

        public int PageIndex
        {
            get => _pageIndex;
            set => _pageIndex = value < 0 ? 0 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value switch
            {
                < 1 => 25,
                > 100 => 100,
                _ => value
            };
        }
        public string SortBy { get; set; } = "Id";
        public string SortOrder { get; set; } = "asc";

        public bool IsDescending => SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

        public override string ToString() =>
            $"SearchTerm='{SearchTerm}', PageIndex={PageIndex}, PageSize={PageSize}, SortBy='{SortBy}', SortOrder='{SortOrder}'";
    }
}
