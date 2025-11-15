namespace Avancira.Application.SubjectCategories.Dtos;

/// <summary>
/// DTO for creating a new subject category.
/// SortOrder is auto-assigned by the backend based on InsertPosition.
/// </summary>
public class SubjectCategoryCreateDto
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Position where to insert the new category.
    /// Options: "start", "end", "custom"
    /// Default: "end"
    /// </summary>
    public string InsertPosition { get; set; } = "end";

    /// <summary>
    /// Custom position (only used when InsertPosition = "custom").
    /// If this position is already taken, backend will find next available position.
    /// </summary>
    public int? CustomPosition { get; set; }

    // REMOVED: SortOrder is now auto-assigned by backend!
}
