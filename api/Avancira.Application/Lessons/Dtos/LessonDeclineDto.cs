namespace Avancira.Application.Lessons.Dtos;

public class LessonDeclineDto
{
    public int LessonId { get; set; }
    public string TutorId { get; set; } = default!;
    public string? Reason { get; set; }
}
