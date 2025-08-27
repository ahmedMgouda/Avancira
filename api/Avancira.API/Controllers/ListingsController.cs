using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Listings.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[ApiController]
[Route("api/listings")]
[Produces("application/json")]
public class ListingsController : BaseApiController
{
    private readonly IListingService _listingService;
    private readonly ILogger<ListingsController> _logger;
    private readonly ICurrentUser _currentUser;

    public ListingsController(IListingService listingService, ILogger<ListingsController> logger, ICurrentUser currentUser)
    {
        _listingService = listingService;
        _logger = logger;
        _currentUser = currentUser;
    }

    [Authorize]
    [HttpGet("tutor-listings")]
    public async Task<IActionResult> GetTutorListings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var userId = _currentUser.GetUserId().ToString();
        var result = await _listingService.GetTutorListingsAsync(userId, page, pageSize, ct);

        return Ok(new
        {
            success = true,
            message = "User listings retrieved successfully.",
            data = result
        });
    }

    [Authorize]
    [HttpPost("create-listing")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] ListingRequestDto model, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(e => e.Value?.Errors.Any() ?? false)
                .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(err => err.ErrorMessage).ToList());

            return BadRequest(new { success = false, errors });
        }

        var userId = _currentUser.GetUserId().ToString();
        var listing = await _listingService.CreateListingAsync(model, userId, ct);

        return CreatedAtAction(nameof(GetListingById), new { id = listing.Id }, new
        {
            success = true,
            message = "Listing created successfully.",
            data = listing
        });
    }

    [Authorize]
    [HttpPut("update-listing/{id:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(Guid id, [FromForm] ListingRequestDto model, CancellationToken ct = default)
    {
        model.Id = id;

        var userId = _currentUser.GetUserId().ToString();
        var updatedListing = await _listingService.UpdateListingAsync(model, userId, ct);

        return Ok(new
        {
            success = true,
            message = "Listing updated successfully.",
            data = updatedListing
        });
    }

    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? query = null,
        [FromQuery] string? categories = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var categoryList = string.IsNullOrWhiteSpace(categories)
            ? new List<string>()
            : categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var results = await _listingService.SearchListingsAsync(query, categoryList, page, pageSize, null, null , 10 , ct);
        return Ok(results);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var userId = _currentUser.GetUserId().ToString();
        var listings = await _listingService.GetTutorListingsAsync(userId, page, pageSize, ct);
        return Ok(listings);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetListingById(Guid id, CancellationToken ct = default)
    {
        var listing = await _listingService.GetListingByIdAsync(id, ct);
        if (listing == null)
            return NotFound($"Listing with ID {id} not found.");

        return Ok(listing);
    }

    [Authorize]
    [HttpPut("{id:guid}/toggle-visibility")]
    public async Task<IActionResult> ToggleVisibility(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId().ToString();
        var success = await _listingService.ToggleListingVisibilityAsync(id, userId, ct);
        if (!success)
            return NotFound($"Listing with ID {id} not found or unauthorized.");

        return Ok(new { success = true, message = "Visibility updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-title")]
    public async Task<IActionResult> UpdateTitle(Guid id, [FromBody] UpdateTitleDto dto, CancellationToken ct = default)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");

        var userId = _currentUser.GetUserId().ToString();
        var success = await _listingService.ModifyListingTitleAsync(id, userId, dto.Title.Trim(), ct);
        if (!success)
            return NotFound("Listing not found or unauthorized.");

        return Ok(new { success = true, message = "Title updated successfully." });
    }

    //[Authorize]
    //[HttpPut("{id:guid}/update-image")]
    //[Consumes("multipart/form-data")]
    //public async Task<IActionResult> UpdateImage(Guid id, [FromForm] IFormFile image, CancellationToken ct = default)
    //{
    //    if (image == null || image.Length == 0)
    //        return BadRequest("Image file is required.");

    //    var userId = GetUserId();
    //    var success = await _listingService.ModifyListingImageAsync(id, userId, image, ct);
    //    if (!success)
    //        return NotFound("Listing not found or unauthorized.");

    //    return Ok(new { success = true, message = "Image updated successfully." });
    //}

    [Authorize]
    [HttpPut("{id:guid}/update-locations")]
    public async Task<IActionResult> UpdateLocations(Guid id, [FromBody] List<string> locations, CancellationToken ct = default)
    {
        if (locations == null || locations.Count == 0)
            return BadRequest("At least one location is required.");

        var userId = _currentUser.GetUserId().ToString();
        var success = await _listingService.ModifyListingLocationsAsync(id, userId, locations, ct);
        if (!success)
            return BadRequest("Failed to update locations.");

        return Ok(new { success = true, message = "Locations updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-description")]
    public async Task<IActionResult> UpdateDescription(Guid id, [FromBody] UpdateDescriptionDto dto, CancellationToken ct = default)
    {
        if (dto == null)
            return BadRequest("Payload is required.");

        var userId = _currentUser.GetUserId().ToString();
        var success = await _listingService.ModifyListingDescriptionAsync(id, userId, dto.AboutLesson, dto.AboutYou, ct);
        if (!success)
            return NotFound("Listing not found or unauthorized.");

        return Ok(new { success = true, message = "Description updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-rates")]
    public async Task<IActionResult> UpdateRates(Guid id, [FromBody] RatesDto dto, CancellationToken ct = default)
    {
        if (dto == null)
            return BadRequest("Payload is required.");

        var userId = _currentUser.GetUserId().ToString();
        var success = await _listingService.ModifyListingRatesAsync(id, userId, dto, ct);
        if (!success)
            return NotFound("Listing not found or unauthorized.");

        return Ok(new { success = true, message = "Rates updated successfully." });
    }

    [Authorize]
    [HttpPut("{id:guid}/update-category")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto, CancellationToken ct = default)
    {
        if (dto == null || dto.LessonCategoryId == Guid.Empty)
            return BadRequest("Valid category is required.");

        var userId = _currentUser.GetUserId().ToString();
        var success = await _listingService.ModifyListingCategoryAsync(id, userId, dto.LessonCategoryId, ct);
        if (!success)
            return NotFound("Listing not found or unauthorized.");

        return Ok(new { success = true, message = "Category updated successfully." });
    }

    [Authorize]
    [HttpDelete("{id:guid}/delete")]
    public async Task<IActionResult> DeleteListing(Guid id, CancellationToken ct = default)
    {
        var listing = await _listingService.GetListingByIdAsync(id, ct);
        if (listing == null)
            return NotFound($"Listing with ID {id} not found.");

        var userId = _currentUser.GetUserId().ToString();
        var success = await _listingService.DeleteListingAsync(id, userId, ct);
        if (!success)
            return BadRequest("Failed to delete listing.");

        return NoContent();
    }
}
