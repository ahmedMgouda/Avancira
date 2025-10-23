namespace Avancira.Application.Identity.Users.Dtos;
public class UserDetailDto
{
    public Guid Id { get; set; }

    public string? UserName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public bool EmailConfirmed { get; set; }

    public string? PhoneNumber { get; set; }

    public Uri? ImageUrl { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Bio { get; set; }

    public string? TimeZoneId { get; set; }

    public string? SkypeId { get; set; }

    public string? HangoutId { get; set; }

    // Address fields
    public AddressDto? Address { get; set; }

    // Payment fields
    public string? PayPalAccountId { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeConnectedAccountId { get; set; }

}

public class AddressDto
{
    public int Id { get; set; }
    public string? FormattedAddress { get; set; }
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? UserId { get; set; }
}
