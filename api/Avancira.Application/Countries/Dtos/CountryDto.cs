namespace Avancira.Application.Countries.Dtos;

public class CountryDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CurrencyCode { get; set; }
    public string? DialingCode { get; set; }
    public bool IsActive { get; set; }
}
