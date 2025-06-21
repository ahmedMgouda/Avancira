using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Catalog.Listings.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Avancira.API.Controllers;

[AllowAnonymous]
[Route("api/listings")]
public class ListingsController : BaseApiController
{
    private readonly IListingService _listingService;
    private readonly ILogger<ListingsController> _logger;

    public ListingsController(
        IListingService listingService,
        ILogger<ListingsController> logger
    )
    {
        _listingService = listingService;
        _logger = logger;
    }

    [Authorize]
    [HttpGet("tutor-listings")]
    public async Task<IActionResult> GetTutorListings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder

        var result = await _listingService.GetTutorListingsAsync(userId, page, pageSize);

        return Ok(new
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

            return BadRequest(new
            {
                success = false,
                errors = errors
            });
        }

        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
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
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        var updatedListing = await _listingService.UpdateListingAsync(model, userId);

        return Ok(new
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
        return Ok(results);
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        var listings = await _listingService.GetTutorListingsAsync(userId, page, pageSize);
        return Ok(listings);
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetListingById(Guid id)
    {
        var listing = _listingService.GetListingById(id);
        if (listing == null)
        {
            return NotFound($"Listing with ID {id} not found.");
        }

        return Ok(listing);
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

        return Ok(new { success = true, message = "Visibility updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-title")]
    public async Task<IActionResult> UpdateTitle(Guid id, [FromBody] UpdateTitleDto updateTitleDto)
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        var success = await _listingService.ModifyListingTitleAsync(id, userId, updateTitleDto.Title);

        if (!success) return NotFound("Listing not found or unauthorized.");

        return Ok(new { success = true, message = "Title updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-image")]
    public async Task<IActionResult> UpdateImage(Guid id, [FromForm] IFormFile image)
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        var success = await _listingService.ModifyListingImageAsync(id, userId, image);

        if (!success) return NotFound("Listing not found or unauthorized.");

        return Ok(new { success = true, message = "Image updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-locations")]
    public async Task<IActionResult> UpdateLocations(Guid id, [FromBody] List<string> locations)
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        var success = await _listingService.ModifyListingLocationsAsync(id, userId, locations);

        if (!success)
        {
            return BadRequest("Failed to update locations.");
        }

        return Ok(new { success = true, message = "Locations updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-description")]
    public async Task<IActionResult> UpdateDescription(Guid id, [FromBody] UpdateDescriptionDto updateDescriptionDto)
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        var success = await _listingService.ModifyListingDescriptionAsync(id, userId, updateDescriptionDto.AboutLesson, updateDescriptionDto.AboutYou);

        if (!success) return NotFound("Listing not found or unauthorized.");

        return Ok(new { success = true, message = "Description updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-rates")]
    public async Task<IActionResult> UpdateRates(Guid id, [FromBody] RatesDto ratesDto)
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        var success = await _listingService.ModifyListingRatesAsync(id, userId, ratesDto);

        if (!success) return NotFound("Listing not found or unauthorized.");

        return Ok(new { success = true, message = "Rates updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-category")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto updateCategoryDto)
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        var success = await _listingService.ModifyListingCategoryAsync(id, userId, updateCategoryDto.LessonCategoryId);

        if (!success) return NotFound("Listing not found or unauthorized.");

        return Ok(new { success = true, message = "Category updated successfully." });
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

        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        var success = await _listingService.DeleteListingAsync(id, userId);
        if (!success)
        {
            return BadRequest("Failed to delete listing.");
        }

        return Ok(new { success = true, message = "Listing deleted successfully." });
    }
}
