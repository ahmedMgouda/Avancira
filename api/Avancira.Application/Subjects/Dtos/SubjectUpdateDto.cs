using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Avancira.Application.Subjects.Dtos;

public record SubjectUpdateDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Subject ID must be positive")]
    public int Id { get; init; }

    [Required(ErrorMessage = "Subject name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; init; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; init; }

    /// <summary>
    /// New icon file upload (replaces existing icon)
    /// </summary>
    public IFormFile? Icon { get; init; }

    /// <summary>
    /// Or provide/keep an icon URL
    /// </summary>
    [Url(ErrorMessage = "IconUrl must be a valid URL")]
    public string? IconUrl { get; init; }

    public bool IsActive { get; init; }

    public bool IsVisible { get; init; }

    public bool IsFeatured { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "Sort order must be non-negative")]
    public int SortOrder { get; init; }

    [Required(ErrorMessage = "Category ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Category ID must be positive")]
    public int CategoryId { get; init; }
}