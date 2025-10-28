using System;

namespace Avancira.Application.Listings.Dtos;

public class ListingDto
{
    public int Id { get; set; }
    public string TutorId { get; set; } = default!;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string? SubjectDescription { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; }
    public bool IsApproved { get; set; }
    public bool IsVisible { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}
