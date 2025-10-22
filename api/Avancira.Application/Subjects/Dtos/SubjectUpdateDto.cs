namespace Avancira.Application.Subjects.Dtos;

public class SubjectUpdateDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? IconUrl { get; set; }

    public bool IsActive { get; set; }
    public bool IsVisible { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }

    public int CategoryId { get; set; }
}
