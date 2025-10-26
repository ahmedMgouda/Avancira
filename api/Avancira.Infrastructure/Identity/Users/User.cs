using Avancira.Domain.Geography;
using Avancira.Domain.Students;
using Avancira.Domain.Tutors;
using Avancira.Domain.Users;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Avancira.Infrastructure.Identity.Users;

public class User : IdentityUser<string>
{
    public User()
    {
        Id = Guid.NewGuid().ToString();
        SecurityStamp = Guid.NewGuid().ToString();
        CreatedOnUtc = DateTime.UtcNow;
    }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    public Uri? ProfileImageUrl { get; set; }

    public bool IsActive { get; set; }

    [MaxLength(100)]
    public string? TimeZoneId { get; set; }

    [Required, MaxLength(3)]
    public string CountryCode { get; set; } = "AU";

    [ForeignKey(nameof(CountryCode))]
    public Country Country { get; set; } = default!;

    [Required, MaxLength(20)]
    public override string? PhoneNumber { get; set; } = string.Empty;

    public Address? Address { get; set; }

    public TutorProfile? TutorProfile { get; set; }

    public StudentProfile? StudentProfile { get; set; }

    public UserPreference? Preference { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? LastModifiedOnUtc { get; set; }

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
