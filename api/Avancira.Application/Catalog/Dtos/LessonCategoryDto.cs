using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog.Dtos
{
    public class LessonCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Image { get; set; }
        public int Courses { get; set; }

        public LessonCategoryDto()
        {
            Name = string.Empty;
        }
    }
}
