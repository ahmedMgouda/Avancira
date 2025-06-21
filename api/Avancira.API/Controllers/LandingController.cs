using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avancira.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Avancira.API.Controllers;

[AllowAnonymous]
[Route("api/landing")]
public class LandingController : BaseApiController
{
    private readonly IListingService _listingService;
    private readonly ILessonCategoryService _categoryService;
    private readonly ILogger<LandingController> _logger;

    public LandingController(
        IListingService listingService,
        ILessonCategoryService categoryService,
        ILogger<LandingController> logger
    )
    {
        _listingService = listingService;
        _categoryService = categoryService;
        _logger = logger;
    }

    [HttpGet("stats")]
    public IActionResult GetCourseStats()
    {
        var stats = _listingService.GetListingStatistics();
        return Ok(stats);
    }

    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        var categories = _categoryService.GetLandingPageCategories();
        return Ok(categories);
    }

    [HttpGet("courses")]
    public IActionResult GetListings()
    {
        var listings = _listingService.GetLandingPageListings()
            .Select(course => new
            {
                listingId = course.Id,
                img = course.ListingImagePath ?? "default-img.jpg",
                lessonCategory = course.LessonCategory,
                title = course.Title,
                description = course.AboutLesson,
                rating = Math.Round(new Random().NextDouble() * (5.0 - 3.5) + 3.5, 1), // Random rating between 3.5 and 5
                reviews = new Random().Next(1000, 5000), // Random reviews between 1000 and 5000
                students = new Random().Next(500, 2000), // Random students between 500 and 2000
                price = $"${course.Rate}",
                instructor = course.TutorName,
                instructorImg = course.ListingImagePath ?? "default-instructor.jpg"
            }).ToList();

        return Ok(listings);
    }

    [HttpGet("trending-courses")]
    public IActionResult GetTrendingListings()
    {
        var listings = _listingService.GetLandingPageTrendingListings()
            .Select(course => new
            {
                listingId = course.Id,
                img = course.ListingImagePath ?? "default-img.jpg",
                lessonCategory = course.LessonCategory,
                title = course.Title,
                description = course.AboutLesson,
                rating = Math.Round(new Random().NextDouble() * (5.0 - 3.5) + 3.5, 1), // Random rating between 3.5 and 5
                reviews = new Random().Next(1000, 5000), // Random reviews between 1000 and 5000
                students = new Random().Next(500, 2000), // Random students between 500 and 2000
                price = $"${course.Rate}",
                instructor = course.TutorName,
                instructorImg = course.ListingImagePath ?? "default-instructor.jpg"
            }).ToList();

        return Ok(listings);
    }

    [HttpGet("instructors")]
    public IActionResult GetInstructors()
    {
        var instructors = new List<object>
        {
            new { Img = "assets/img/mentor/amr_mostafa.jpg", Name = "Amr Mostafa", Designation = "Software Engineer & AI Specialist", Rating = 5.0, Reviews = 2566, Students = 800 },
            new { Img = "assets/img/mentor/amir_salah.jpg", Name = "Amir Salah", Designation = "Business Strategist & Finance Expert", Rating = 4.8, Reviews = 2550, Students = 700 },
            new { Img = "assets/img/mentor/ahmed_mostafa.jpg", Name = "Ahmed Mostafa", Designation = "Creative Director & Multimedia Specialist", Rating = 4.5, Reviews = 2500, Students = 850 }
        };

        return Ok(instructors);
    }

    [HttpGet("job-locations")]
    public async Task<IActionResult> GetJobLocationsAsync()
    {
        // TODO: Implement proper user service for landing page data
        var jobLocations = new List<object>
        {
            new { Img = "assets/img/city/city_sydney.jpg", City = "Sydney", Country = "Australia", Mentors = 0 },
            new { Img = "assets/img/city/city_brisbane.jpg", City = "Brisbane", Country = "Australia", Mentors = 0 },
            new { Img = "assets/img/city/city_perth.jpg", City = "Perth", Country = "Australia", Mentors = 0 }
        };
        return Ok(jobLocations);
    }

    [HttpGet("student-reviews")]
    public IActionResult GetStudentReviews()
    {
        var studentReviews = new List<object>
        {
            new { Img = "assets/img/user/user20.png", Name = "Hannah Schmitt", Position = "Lead Designer", Comment = "Great experience, highly recommend!", Rating = 5 },
            new { Img = "assets/img/user/user21.png", Name = "Anderson Saviour", Position = "IT Manager", Comment = "Very insightful lessons and great support.", Rating = 4 }
        };

        return Ok(studentReviews);
    }
}
