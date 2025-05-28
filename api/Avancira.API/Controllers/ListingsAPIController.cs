using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Catalog.Listings.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers;

[Route("api/listings")]
[ApiController]
public class ListingsAPIController : BaseController
{
    private readonly IListingService _listingService;
    private readonly ILogger<ListingsAPIController> _logger;

    public ListingsAPIController(
        IListingService listingService,
        ILogger<ListingsAPIController> logger
    )
    {
        _listingService = listingService;
        _logger = logger;
    }

    [Authorize]
    [HttpGet("tutor-listings")]
    public async Task<IActionResult> GetTutorListings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = Guid.Parse(GetUserId());

        var result = await _listingService.GetTutorListingsAsync(userId, page, pageSize);

        return JsonOk(new
        {
            success = true,
            message = "User listings retrieved successfully.",
            data = result
        });
    }

    [Authorize]
    [HttpPost("create-listing")]
    public async Task<IActionResult> Create([FromForm] ListingRequestDto model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(e => e.Value?.Errors.Any() ?? false)
                .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(err => err.ErrorMessage).ToList());

            return JsonError("Validation failed.", new
            {
                success = false,
                errors = errors
            });
        }

        var userId = Guid.Parse(GetUserId());
        var listing = await _listingService.CreateListingAsync(model, userId);

        return CreatedAtAction(nameof(GetListingById), new { id = listing.Id },
            new
            {
                success = true,
                message = "Listing created successfully.",
                data = listing
            });
    }

    [Authorize]
    [HttpPut("update-listing/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromForm] ListingRequestDto model)
    {
        model.Id = id;
        var userId = Guid.Parse(GetUserId());
        var updatedListing = await _listingService.UpdateListingAsync(model, userId);

        return JsonOk(new
        {
            success = true,
            message = "Listing updated successfully.",
            data = updatedListing
        });
    }

    // Read
    [HttpGet("search")]
    public IActionResult Search([FromQuery] string? query = null, [FromQuery] string? categories = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var categoryList = string.IsNullOrWhiteSpace(categories)
            ? new List<string>()
            : categories.Split(',').Select(c => c.Trim()).ToList();

        var results = _listingService.SearchListings(query!, categoryList, page, pageSize);
        return JsonOk(results);
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = Guid.Parse(GetUserId());
        var listings = await _listingService.GetTutorListingsAsync(userId, page, pageSize);
        return JsonOk(listings);
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetListingById(Guid id)
    {
        var listing = _listingService.GetListingById(id);
        if (listing == null)
        {
            return NotFound($"Listing with ID {id} not found.");
        }

        return JsonOk(listing);
    }

    // Update
    [Authorize]
    [HttpPut("{id:guid}/toggle-visibility")]
    public async Task<IActionResult> ToggleVisibility(Guid id)
    {
        var success = await _listingService.ToggleListingVisibilityAsync(id);
        if (!success)
        {
            return NotFound($"Listing with ID {id} not found.");
        }

        return JsonOk(new { success = true, message = "Visibility updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-title")]
    public async Task<IActionResult> UpdateTitle(Guid id, [FromBody] UpdateTitleDto updateTitleDto)
    {
        var userId = Guid.Parse(GetUserId());
        var success = await _listingService.ModifyListingTitleAsync(id, userId, updateTitleDto.Title);

        if (!success) return NotFound("Listing not found or unauthorized.");

        return JsonOk(new { success = true, message = "Title updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-image")]
    public async Task<IActionResult> UpdateImage(Guid id, [FromForm] IFormFile image)
    {
        var userId = Guid.Parse(GetUserId());
        var success = await _listingService.ModifyListingImageAsync(id, userId, image);

        if (!success) return NotFound("Listing not found or unauthorized.");

        return JsonOk(new { success = true, message = "Image updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-locations")]
    public async Task<IActionResult> UpdateLocations(Guid id, [FromBody] List<string> locations)
    {
        var userId = Guid.Parse(GetUserId());
        var success = await _listingService.ModifyListingLocationsAsync(id, userId, locations);

        if (!success)
        {
            return BadRequest("Failed to update locations.");
        }

        return JsonOk(new { success = true, message = "Locations updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-description")]
    public async Task<IActionResult> UpdateDescription(Guid id, [FromBody] UpdateDescriptionDto updateDescriptionDto)
    {
        var userId = Guid.Parse(GetUserId());
        var success = await _listingService.ModifyListingDescriptionAsync(id, userId, updateDescriptionDto.AboutLesson, updateDescriptionDto.AboutYou);

        if (!success) return NotFound("Listing not found or unauthorized.");

        return JsonOk(new { success = true, message = "Description updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-rates")]
    public async Task<IActionResult> UpdateRates(Guid id, [FromBody] RatesDto ratesDto)
    {
        var userId = Guid.Parse(GetUserId());
        var success = await _listingService.ModifyListingRatesAsync(id, userId, ratesDto);

        if (!success) return NotFound("Listing not found or unauthorized.");

        return JsonOk(new { success = true, message = "Rates updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-category")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto updateCategoryDto)
    {
        var userId = Guid.Parse(GetUserId());
        var success = await _listingService.ModifyListingCategoryAsync(id, userId, updateCategoryDto.LessonCategoryId);

        if (!success) return NotFound("Listing not found or unauthorized.");

        return JsonOk(new { success = true, message = "Category updated successfully." });
    }

    // Delete
    [Authorize]
    [HttpDelete("{id:guid}/delete")]
    public async Task<IActionResult> DeleteListing(Guid id)
    {
        var listing = _listingService.GetListingById(id);
        if (listing == null)
        {
            return NotFound($"Listing with ID {id} not found.");
        }

        var userId = Guid.Parse(GetUserId());
        var success = await _listingService.DeleteListingAsync(id, userId);
        if (!success)
        {
            return JsonError("Failed to delete listing.");
        }

        return JsonOk(new { success = true, message = "Listing deleted successfully." });
    }
}
