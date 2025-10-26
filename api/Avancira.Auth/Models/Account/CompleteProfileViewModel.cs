using Microsoft.AspNetCore.Mvc.Rendering;

namespace Avancira.Auth.Models.Account;

public class CompleteProfileViewModel
{
    public string Provider { get; set; } = default!;
    public string ProviderKey { get; set; } = default!;

    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string CountryCode { get; set; } = default!;
    public string TimeZoneId { get; set; } = default!;

    public string ReturnUrl { get; set; } = "/connect/authorize";

    public IEnumerable<SelectListItem>? Countries { get; set; }
    public IEnumerable<SelectListItem>? TimeZones { get; set; }
}
