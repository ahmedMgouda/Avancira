using System;
using System.Text.Json.Serialization;

namespace Avancira.Application.Identity.Users.Dtos;

public class RegisterUserDto
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "AU";
    public string? Gender { get; set; }
    public string? TimeZoneId { get; set; }
    public string? ReferralToken { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool RegisterAsTutor { get; set; }
    public bool RegisterAsStudent { get; set; } = true;

    [JsonPropertyName("acceptTerms")]
    public bool AcceptTerms { get; set; }

    [JsonIgnore]
    public string? Origin { get; set; }
}