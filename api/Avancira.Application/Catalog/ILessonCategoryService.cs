using Avancira.Application.Catalog.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog
{
    public interface ILessonCategoryService
    {
        // Read
        List<LessonCategoryDto> GetLandingPageCategories();
    }
}
