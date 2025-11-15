using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;
using Avancira.Domain.Subjects;


namespace Avancira.Application.SubjectCategories.Specifications
{
    public sealed class CategoryBySortOrderSpec : Specification<SubjectCategory>
    {
        public CategoryBySortOrderSpec(int sortOrder)
        {
            Query.Where(c => c.SortOrder == sortOrder);
        }
    }
}
