namespace Avancira.Application.Lessons.Dtos;

public class LessonCancelDto
{
    public int LessonId { get; set; }
    public string CanceledBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
