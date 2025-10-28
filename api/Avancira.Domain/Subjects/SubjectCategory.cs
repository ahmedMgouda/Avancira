using System.Collections.Generic;
using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;

namespace Avancira.Domain.Subjects;

public class SubjectCategory : BaseEntity<int>, IAggregateRoot
{
    private SubjectCategory()
    {
    }

    private SubjectCategory(
        string name,
        string? description,
        bool isActive,
        bool isVisible,
        bool isFeatured,
        int sortOrder)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
        IsVisible = isVisible;
        IsFeatured = isFeatured;
        SortOrder = sortOrder;
    }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsVisible { get; private set; } = true;
    public bool IsFeatured { get; private set; } = false;
    public int SortOrder { get; private set; }
    public ICollection<Subject> Subjects { get; private set; } = new List<Subject>();

    public static SubjectCategory Create(
        string name,
        string? description,
        bool isActive,
        bool isVisible,
        bool isFeatured,
        int sortOrder) =>
        new(name, description, isActive, isVisible, isFeatured, sortOrder);

    public void Update(
        string name,
        string? description,
        bool isActive,
        bool isVisible,
        bool isFeatured,
        int sortOrder)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
        IsVisible = isVisible;
        IsFeatured = isFeatured;
        SortOrder = sortOrder;
    }
}
