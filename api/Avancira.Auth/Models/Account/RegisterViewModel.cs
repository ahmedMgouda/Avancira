using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Avancira.Auth.Models.Account
{
    public class RegisterViewModel
    {
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Email Address")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Country")]
        public string CountryCode { get; set; } = "US";

        [Display(Name = "Time Zone")]
        public string? TimeZoneId { get; set; }

        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateOnly? DateOfBirth { get; set; }

        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "I accept the Terms and Conditions")]
        public bool AcceptTerms { get; set; }

        [Display(Name = "Register as Tutor")]
        public bool RegisterAsTutor { get; set; }

        public IEnumerable<SelectListItem> Countries { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> TimeZones { get; set; } = Enumerable.Empty<SelectListItem>();

        public string? ReturnUrl { get; set; }
    }
}
