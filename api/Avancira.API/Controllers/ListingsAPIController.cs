//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Avancira.Application.Catalog.Dtos;
//using Avancira.Application.Common;
//using Avancira.Domain.Catalog;
//using Avancira.Domain.Messaging;
//using Hangfire.MemoryStorage.Database;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Http.HttpResults;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using static System.Runtime.InteropServices.JavaScript.JSType;

//namespace Backend.Controllers;

//[Route("api/listings")]
//[ApiController]
//public class ListingsAPIController : BaseController
//{
//    private readonly IListingService _listingService;
//    private readonly ILogger<ListingsAPIController> _logger;

//    public ListingsAPIController(
//        IListingService listingService,
//        ILogger<ListingsAPIController> logger
//    )
//    {
//        _listingService = listingService;
//        _logger = logger;
//    }

//    [Authorize]
//    [HttpGet("tutor-listings")]
//    public async Task<IActionResult> GetTutorListings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
//    {
//        var userId = GetUserId();

//        var result = await _listingService.GetTutorListingsAsync(userId, page, pageSize);

//        return JsonOk(new
//        {
//            success = true,
//            message = "User listings retrieved successfully.",
//            data = result
//        });
//    }

//    [Authorize]
//    [HttpPost("create-listing")]
//    public async Task<IActionResult> Create([FromForm] ListingRequestDto model)
//    {
//        if (!ModelState.IsValid)
//        {
//            var errors = ModelState
//                .Where(e => e.Value?.Errors.Any() ?? false)
//                .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(err => err.ErrorMessage).ToList());

//            return JsonError("Validation failed.", new
//            {
//                success = false,
//                errors = errors
//            });
//        }

//        var userId = GetUserId();
//        var listing = await _listingService.CreateListingAsync(model, userId);

//        return CreatedAtAction(nameof(GetListingById), new { id = listing.Id },
//            new
//            {
//                success = true,
//                message = "Listing created successfully.",
//                data = listing
//            });
//    }

//    [Authorize]
//    [HttpPut("update-listing/{id}")]
//    public async Task<IActionResult> Update([FromForm] ListingRequestDto model)
//    {
//        var userId = GetUserId();
//        var updatedListing = await _listingService.UpdateListingAsync(model, userId);

//        return Ok(
//            new
//            {
//                success = true,
//                message = "Listing updated successfully.",
//                data = updatedListing
//            });
//    }


//    // Read
//    [HttpGet("search")]
//    public IActionResult Search([FromQuery] string? query = null, [FromQuery] string? categories = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
//    {
//        var categoryList = string.IsNullOrWhiteSpace(categories)
//            ? new List<string>()
//            : categories.Split(',').Select(c => c.Trim()).ToList();

//        var results = _listingService.SearchListings(query, categoryList, page, pageSize);
//        return JsonOk(results);
//    }

//    [HttpGet()]
//    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
//    {
//        var userId = GetUserId();
//        var listings = await _listingService.GetTutorListingsAsync(userId, page, pageSize);
//        return JsonOk(listings);
//    }

//    [HttpGet("{id:int}")]
//    public IActionResult GetListingById(int id)
//    {
//        var listing = _listingService.GetListingById(id);
//        if (listing == null)
//        {
//            return NotFound($"Listing with ID {id} not found.");
//        }

//        return JsonOk(listing);
//    }


//    // Update
//    [Authorize]
//    [HttpPut("{id:int}/toggle-visibility")]
//    public IActionResult ToggleVisibility(int id)
//    {
//        var listing = _listingService.GetListingById(id);
//        if (listing == null)
//        {
//            return NotFound($"Listing with ID {id} not found.");
//        }

//        // Toggle visibility logic
//        var updatedListing = _listingService.ToggleListingVisibilityAsync(id);
//        return JsonOk(new { success = true, message = "Visibility updated successfully." });
//    }

//    [Authorize]
//    [HttpPut("{id:int}/update-title")]
//    public async Task<IActionResult> UpdateTitle(int id, [FromBody] UpdateTitleDto updateTitleDto)
//    {
//        var userId = GetUserId();
//        var success = await _listingService.ModifyListingTitleAsync(id, userId, updateTitleDto.Title);

//        if (!success) return NotFound("Listing not found or unauthorized.");

//        return JsonOk(new { success = true, message = "Title updated successfully." });
//    }

//    [Authorize]
//    [HttpPut("{id:int}/update-image")]
//    public async Task<IActionResult> UpdateImage(int id, [FromForm] IFormFile image)
//    {
//        var userId = GetUserId();
//        var success = await _listingService.ModifyListingImageAsync(id, userId, image);

//        if (!success) return NotFound("Listing not found or unauthorized.");

//        return JsonOk(new { success = true, message = "Image updated successfully." });
//    }

//    [Authorize]
//    [HttpPut("{id:int}/update-locations")]
//    public async Task<IActionResult> UpdateLocations(int id, [FromBody] List<string> locations)
//    {
//        var userId = GetUserId();
//        var success = await _listingService.ModifyListingLocationsAsync(id, userId, locations);

//        if (!success)
//        {
//            return BadRequest("Failed to update locations.");
//        }

//        return JsonOk(new { success = true, message = "Locations updated successfully." });
//    }

//    [Authorize]
//    [HttpPut("{id:int}/update-description")]
//    public async Task<IActionResult> UpdateDescription(int id, [FromBody] UpdateDescriptionDto updateDescriptionDto)
//    {
//        var userId = GetUserId();
//        var success = await _listingService.ModifyListingDescriptionAsync(id, userId, updateDescriptionDto.AboutLesson, updateDescriptionDto.AboutYou);

//        if (!success) return NotFound("Listing not found or unauthorized.");

//        return JsonOk(new { success = true, message = "Description updated successfully." });
//    }

//    [Authorize]
//    [HttpPut("{id:int}/update-rates")]
//    public async Task<IActionResult> UpdateRates(int id, [FromBody] RatesDto ratesDto)
//    {
//        var userId = GetUserId();
//        var success = await _listingService.ModifyListingRatesAsync(id, userId, ratesDto);

//        if (!success) return NotFound("Listing not found or unauthorized.");

//        return JsonOk(new { success = true, message = "Rates updated successfully." });
//    }

//    [Authorize]
//    [HttpPut("{id:int}/update-category")]
//    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto updateCategoryDto)
//    {
//        var userId = GetUserId();
//        var success = await _listingService.ModifyListingCategoryAsync(id, userId, updateCategoryDto.LessonCategoryId);

//        if (!success) return NotFound("Listing not found or unauthorized.");

//        return JsonOk(new { success = true, message = "Category updated successfully." });
//    }

//    // Delete
//    [Authorize]
//    [HttpDelete("{id:int}/delete")]
//    public IActionResult DeleteListing(int id)
//    {
//        var listing = _listingService.GetListingById(id);
//        if (listing == null)
//        {
//            return NotFound($"Listing with ID {id} not found.");
//        }

//        // Toggle visibility logic
//        var userId = GetUserId();
//        var deletedListing = _listingService.DeleteListingAsync(id, userId);
//        return JsonOk(new { success = true, message = "Listing deleted successfully." });
//    }
//}


