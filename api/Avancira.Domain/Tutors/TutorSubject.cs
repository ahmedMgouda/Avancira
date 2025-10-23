using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;
using Avancira.Domain.Subjects;

namespace Avancira.Domain.Tutors;

public class TutorSubject : BaseEntity<int>, IAggregateRoot
{
    private TutorSubject()
    {
    }

    private TutorSubject(
        string tutorId,
        int subjectId,
        decimal hourlyRate,
        bool isActive,
        int sortOrder)
    {
        TutorId = tutorId;
        SubjectId = subjectId;
        HourlyRate = hourlyRate;
        IsActive = isActive;
        SortOrder = sortOrder;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string TutorId { get; private set; } = default!;
    public TutorProfile Tutor { get; private set; } = default!;
    public int SubjectId { get; private set; }
    public Subject Subject { get; private set; } = default!;
    public decimal HourlyRate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? DeactivatedOnUtc { get; private set; }
    public int SortOrder { get; private set; }
    public double AverageRating { get; private set; }
    public int TotalReviews { get; private set; }
    public bool IsApproved { get; private set; } = true;
    public bool IsVisible { get; private set; } = true;
    public bool IsFeatured { get; private set; }
    public string? AdminComment { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    public static TutorSubject Create(
        string tutorId,
        int subjectId,
        decimal hourlyRate,
        bool isActive = true,
        int sortOrder = 0) =>
        new(tutorId, subjectId, hourlyRate, isActive, sortOrder);

    public void Update(decimal hourlyRate, bool isActive, int sortOrder)
    {
        HourlyRate = hourlyRate;
        SortOrder = sortOrder;

        if (IsActive && !isActive)
        {
            DeactivatedOnUtc = DateTime.UtcNow;
        }

        IsActive = isActive;
    }

    public void SetApproval(bool isApproved, string? adminComment)
    {
        IsApproved = isApproved;
        AdminComment = adminComment;
    }

    public void SetVisibility(bool isVisible, bool isFeatured)
    {
        IsVisible = isVisible;
        IsFeatured = isFeatured;
    }

    public void UpdateRatings(double averageRating, int totalReviews)
    {
        AverageRating = averageRating;
        TotalReviews = totalReviews;
    }
}
