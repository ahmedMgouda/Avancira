using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers;

[AllowAnonymous]
[Route("api/landing")]
[ApiController]
public class LandingAPIController : BaseController
{
    private readonly IListingService _listingService;
    private readonly IUserService _userService;
    private readonly ILessonCategoryService _categoryService;
    private readonly ILogger<LandingAPIController> _logger;

    public LandingAPIController(
        IListingService listingService,
        IUserService userService,
        ILessonCategoryService categoryService,
        ILogger<LandingAPIController> logger
    )
    {
        _listingService = listingService;
        _userService = userService;
        _categoryService = categoryService;
        _logger = logger;
    }

    [HttpGet("stats")]
    public IActionResult GetCourseStats()
    {
        var stats = _listingService.GetListingStatistics();
        return JsonOk(stats);
    }

    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        var categories = _categoryService.GetLandingPageCategories();
        return JsonOk(categories);
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

        return JsonOk(listings);
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

        return JsonOk(listings);
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

        return JsonOk(instructors);
    }

    [HttpGet("job-locations")]
    public async Task<IActionResult> GetJobLocationsAsync()
    {
        var jobLocations = await _userService.GetLandingPageUsersAsync();
        return JsonOk(jobLocations);
    }

    [HttpGet("student-reviews")]
    public IActionResult GetStudentReviews()
    {
        var studentReviews = new List<object>
        {
            new { Img = "assets/img/user/user20.png", Name = "Hannah Schmitt", Position = "Lead Designer", Comment = "Great experience, highly recommend!", Rating = 5 },
            new { Img = "assets/img/user/user21.png", Name = "Anderson Saviour", Position = "IT Manager", Comment = "Very insightful lessons and great support.", Rating = 4 }
        };

        return JsonOk(studentReviews);
    }
}

