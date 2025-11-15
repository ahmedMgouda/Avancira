using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Avancira.Application.Subjects.Dtos;

public record SubjectCreateDto
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    /// <summary>
    /// Icon file upload (IFormFile from multipart/form-data)
    /// </summary>
    public IFormFile? Icon { get; init; }

    public string? IconUrl { get; init; }

    public bool IsActive { get; init; } = true;

    public bool IsVisible { get; init; } = true;

    public bool IsFeatured { get; init; } = false;

    public int SortOrder { get; init; } = 0;
    public int CategoryId { get; init; }
}
