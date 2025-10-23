namespace Avancira.Application.TutorSubjects.Dtos;

public class TutorSubjectUpdateDto
{
    public int Id { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
