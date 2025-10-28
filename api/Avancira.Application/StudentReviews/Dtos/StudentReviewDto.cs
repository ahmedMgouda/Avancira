using Avancira.Domain.Reviews;

namespace Avancira.Application.StudentReviews.Dtos;

public class StudentReviewDto
{
    public int Id { get; set; }
    public string StudentId { get; set; } = default!;
    public int LessonId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int? CommunicationRating { get; set; }
    public int? KnowledgeRating { get; set; }
    public int? ProfessionalismRating { get; set; }
    public int? ValueRating { get; set; }
    public bool IsApproved { get; set; }
    public bool IsFlagged { get; set; }
    public string? TutorResponse { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}
