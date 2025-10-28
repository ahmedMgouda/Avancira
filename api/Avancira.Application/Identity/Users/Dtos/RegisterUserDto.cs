namespace Avancira.Application.Identity.Users.Dtos;

public sealed class RegisterUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public string CountryCode { get; set; } = "AU"; // ISO 2-letter country code
    public string? Gender { get; set; } // "Male", "Female", "Other"
    public DateOnly? DateOfBirth { get; set; }
    public string? TimeZoneId { get; set; }

    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool RegisterAsTutor { get; set; }

    public bool AcceptTerms { get; set; }
}
