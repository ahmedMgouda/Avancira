namespace Avancira.Application.Listings.Dtos;

public class ListingUpdateDto
{
    public int Id { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
