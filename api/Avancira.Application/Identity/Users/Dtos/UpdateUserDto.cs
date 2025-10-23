using Avancira.Application.Storage.File.Dtos;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Avancira.Application.Identity.Users.Dtos;
public class UpdateUserDto
{
    public string Id { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? DateOfBirth { get; set; }
    public string? SkypeId { get; set; }
    public string? HangoutId { get; set; }
    [MaxLength(500)]
    public string? Bio { get; set; }
    public string? TimeZoneId { get; set; }
    public IFormFile? Image { get; set; }
    public bool DeleteCurrentImage { get; set; }
    
    // Address fields
    public string? AddressFormattedAddress { get; set; }
    public string? AddressStreetAddress { get; set; }
    public string? AddressCity { get; set; }
    public string? AddressState { get; set; }
    public string? AddressCountry { get; set; }
    public string? AddressPostalCode { get; set; }
    public double? AddressLatitude { get; set; }
    public double? AddressLongitude { get; set; }
    
    // Profile verification and stats
    public string? ProfileVerified { get; set; } // Comma-separated string
    public string? RecommendationToken { get; set; }
    public bool? IsStripeConnected { get; set; }
}
