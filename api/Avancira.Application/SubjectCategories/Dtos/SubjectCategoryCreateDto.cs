namespace Avancira.Application.SubjectCategories.Dtos;

public class SubjectCategoryCreateDto
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public int SortOrder { get; set; } = 0;
}
