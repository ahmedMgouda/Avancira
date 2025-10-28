namespace Avancira.Application.StudentReviews.Dtos;

public class StudentReviewCreateDto
{
    public string StudentId { get; set; } = default!;
    public int LessonId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int? CommunicationRating { get; set; }
    public int? KnowledgeRating { get; set; }
    public int? ProfessionalismRating { get; set; }
    public int? ValueRating { get; set; }
}
