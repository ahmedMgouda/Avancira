namespace Avancira.Application.Listings.Dtos;

public class ListingCreateDto
{
    public string TutorId { get; set; } = default!;
    public int SubjectId { get; set; }
    public decimal HourlyRate { get; set; }
    public int SortOrder { get; set; }
}
