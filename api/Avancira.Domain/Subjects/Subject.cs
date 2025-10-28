using System;
using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;

namespace Avancira.Domain.Subjects;

public class Subject : BaseEntity<int>, IAggregateRoot
{
    private Subject()
    {
    }

    private Subject(
        string name,
        string? description,
        string? iconUrl,
        bool isActive,
        bool isVisible,
        bool isFeatured,
        int sortOrder,
        int categoryId)
    {
        Name = name;
        Description = description;
        IconUrl = iconUrl;
        IsActive = isActive;
        IsVisible = isVisible;
        IsFeatured = isFeatured;
        SortOrder = sortOrder;
        CategoryId = categoryId;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? IconUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsVisible { get; private set; } = true;
    public bool IsFeatured { get; private set; } = false;
    public int SortOrder { get; private set; }
    public int CategoryId { get; private set; }
    public SubjectCategory Category { get; private set; } = default!;
    public DateTime CreatedOnUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedOnUtc { get; private set; }
    public static Subject Create(
        string name,
        string? description,
        string? iconUrl,
        bool isActive,
        bool isVisible,
        bool isFeatured,
        int sortOrder,
        int categoryId) =>
        new(name, description, iconUrl, isActive, isVisible, isFeatured, sortOrder, categoryId);

    public void Update(
        string name,
        string? description,
        string? iconUrl,
        bool isActive,
        bool isVisible,
        bool isFeatured,
        int sortOrder,
        int categoryId)
    {
        Name = name;
        Description = description;
        IconUrl = iconUrl;
        IsActive = isActive;
        IsVisible = isVisible;
        IsFeatured = isFeatured;
        SortOrder = sortOrder;
        CategoryId = categoryId;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
