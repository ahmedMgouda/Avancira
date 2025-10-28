using System;
using System.Collections.Generic;
using Avancira.Application.Countries;
using Avancira.Application.Countries.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/countries")]
public class CountriesController : BaseApiController
{
    private readonly ICountryService _countryService;

    public CountriesController(ICountryService countryService)
    {
        _countryService = countryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CountryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCountries(CancellationToken cancellationToken)
    {
        var countries = await _countryService.GetAllAsync(cancellationToken);
        return Ok(countries);
    }

    [HttpGet("{code}")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCountry(string code, CancellationToken cancellationToken)
    {
        var country = await _countryService.GetByCodeAsync(code, cancellationToken);
        return Ok(country);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCountry([FromBody] CountryCreateDto request, CancellationToken cancellationToken)
    {
        var country = await _countryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCountry), new { code = country.Code }, country);
    }

    [HttpPut("{code}")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCountry(string code, [FromBody] CountryUpdateDto request, CancellationToken cancellationToken)
    {
        if (!string.Equals(code, request.Code, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Country code mismatch.");
        }

        var country = await _countryService.UpdateAsync(request, cancellationToken);
        return Ok(country);
    }

    [HttpDelete("{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCountry(string code, CancellationToken cancellationToken)
    {
        await _countryService.DeleteAsync(code, cancellationToken);
        return NoContent();
    }
}
