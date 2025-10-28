using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.Listings;
using Avancira.Application.Listings.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/listings")]
public class ListingsController : BaseApiController
{
    private readonly IListingService _listingService;

    public ListingsController(IListingService listingService)
    {
        _listingService = listingService;
    }

    [HttpGet("{tutorId}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ListingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetListings(string tutorId, CancellationToken cancellationToken)
    {
        var listings = await _listingService.GetByTutorIdAsync(tutorId, cancellationToken);
        return Ok(listings);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ListingDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateListing([FromBody] ListingCreateDto request, CancellationToken cancellationToken)
    {
        var listing = await _listingService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetListings), new { tutorId = listing.TutorId }, listing);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ListingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateListing(int id, [FromBody] ListingUpdateDto request, CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return BadRequest("Listing ID mismatch.");
        }

        var listing = await _listingService.UpdateAsync(request, cancellationToken);
        return Ok(listing);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteListing(int id, CancellationToken cancellationToken)
    {
        await _listingService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
