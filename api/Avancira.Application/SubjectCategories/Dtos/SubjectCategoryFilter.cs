using Avancira.Application.Common.Models;

namespace Avancira.Application.SubjectCategories.Dtos;

public sealed class SubjectCategoryFilter : BaseFilter
{
    public bool? IsActive { get; set; }
    public bool? IsVisible { get; set; }
    public bool? IsFeatured { get; set; }
}