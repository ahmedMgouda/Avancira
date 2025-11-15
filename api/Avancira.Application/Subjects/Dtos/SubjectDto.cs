namespace Avancira.Application.Subjects.Dtos;

public record SubjectDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public bool IsActive { get; init; }
    public bool IsVisible { get; init; }
    public bool IsFeatured { get; init; }
    public int SortOrder { get; init; }
    public int CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
