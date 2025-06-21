using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog.Dtos
{
    public class UserDto
    {
        public string? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? Bio { get; set; }
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfileImagePath { get; set; }
        public IFormFile? ProfileImage { get; set; }
        public AddressDto? Address { get; set; }
        public string? TimeZoneId { get; set; } = "Austarlia/Sydney";
        public string? SkypeId { get; set; }
        public string? HangoutId { get; set; }
        public string? RecommendationToken { get; set; }
        public List<string>? ProfileVerified { get; set; }
        public bool? IsStripeConnected { get; set; }
        public bool? IsPayPalConnected { get; set; }

        // Analytics
        public int? LessonsCompleted { get; set; }
        public int? Evaluations { get; set; }
        public int ProfileCompletion { get; set; }
    }
}
