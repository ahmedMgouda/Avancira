using System;

namespace Avancira.Application.Identity.Users.Dtos;

public class UserDetailDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public string? CountryCode { get; set; }
    public string? CountryName { get; set; }
    public string? DialingCode { get; set; }
    public string? Gender { get; set; }
    public Uri? ProfileImageUrl { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? TimeZoneId { get; set; }
    public AddressDto? Address { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? LastModifiedOnUtc { get; set; }
}

public class AddressDto
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
}
