namespace Avancira.Application.TutorSubjects.Dtos;

public class TutorSubjectCreateDto
{
    public string TutorId { get; set; } = default!;
    public int SubjectId { get; set; }
    public decimal HourlyRate { get; set; }
    public int SortOrder { get; set; }
}
