using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;
using Avancira.Domain.Subjects;


namespace Avancira.Application.SubjectCategories.Specifications
{
    public sealed class CategoryByIdsSpec : Specification<SubjectCategory>
    {
        public CategoryByIdsSpec(int[] ids)
        {
            Query.Where(c => ids.Contains(c.Id));
        }
    }
}
