namespace Avancira.Application.Identity.Users.Dtos;

public sealed class SocialRegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
    public string? CountryCode { get; set; } = "AU";
    public string? Gender { get; set; }
    public string? TimeZoneId { get; set; }

    public bool RegisterAsTutor { get; set; }
}
