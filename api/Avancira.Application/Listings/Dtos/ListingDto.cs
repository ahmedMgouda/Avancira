using Avancira.Application.Catalog.Dtos;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Listings.Dtos
{
    public class ListingDto
    {
        public Guid Id { get; set; }
        public string TutorId { get; set; }
        public string TutorName { get; set; }
        public string TutorBio { get; set; }
        public int ContactedCount { get; set; }
        public AddressDto? TutorAddress { get; set; }
        public int Reviews { get; set; }
        public string LessonCategory { get; set; }
        public string Title { get; set; }
        public string? ListingImagePath { get; set; }
        public IFormFile? ListingImage { get; set; }
        public List<string> Locations { get; set; }
        public string AboutLesson { get; set; }
        public string AboutYou { get; set; }
        public string Rate { get; set; }
        public RatesDto Rates { get; set; }
        public List<string> SocialPlatforms { get; set; }
        public bool IsVisible { get; set; }

        public ListingDto()
        {
            TutorName = string.Empty;
            TutorId = string.Empty;
            TutorBio = string.Empty;
            LessonCategory = string.Empty;
            Title = string.Empty;
            AboutLesson = string.Empty;
            AboutYou = string.Empty;
            Rate = string.Empty;
            Locations = new List<string>();
            SocialPlatforms = new List<string>();
            Rates = new RatesDto();
        }
    }
}
