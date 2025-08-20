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
    public string? PhoneNumber { get; set; }
    public string? TimeZoneId { get; set; }
    public string? ReferralToken { get; set; }
    public bool AcceptTerms { get; set; }

    [JsonIgnore]
    public string? Origin { get; set; }
}
