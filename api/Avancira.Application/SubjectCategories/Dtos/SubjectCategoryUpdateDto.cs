namespace Avancira.Application.SubjectCategories.Dtos;

/// <summary>
/// DTO for updating an existing subject category.
/// SortOrder is NOT editable via update - use Reorder/Move endpoints instead.
/// </summary>
public class SubjectCategoryUpdateDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
    public bool IsVisible { get; set; }
    public bool IsFeatured { get; set; }

    // REMOVED: SortOrder
    // To change order, use:
    // - PUT /api/subject-categories/reorder (drag-drop)
    // - PUT /api/subject-categories/{id}/move (move to position)
}
