using Avancira.Application.Countries.Dtos;
using Avancira.Application.Countries.Specifications;
using Avancira.Application.Persistence;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Geography;
using Mapster;

namespace Avancira.Application.Countries;

public sealed class CountryService : ICountryService
{
    private readonly IRepository<Country> _countryRepository;

    public CountryService(IRepository<Country> countryRepository)
    {
        _countryRepository = countryRepository;
    }

    public async Task<IReadOnlyCollection<CountryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var countries = await _countryRepository.ListAsync(cancellationToken);
        return countries
            .OrderBy(c => c.Name)
            .Adapt<IReadOnlyCollection<CountryDto>>();
    }

    public async Task<CountryDto> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var spec = new CountryByCodeSpec(code);
        var country = await _countryRepository.FirstOrDefaultAsync(spec, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Country '{code}' not found.");

        return country.Adapt<CountryDto>();
    }

    public async Task<CountryDto> CreateAsync(CountryCreateDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var code = dto.Code.ToUpperInvariant();
        var existingSpec = new CountryByCodeSpec(code);
        var existing = await _countryRepository.FirstOrDefaultAsync(existingSpec, cancellationToken);

        if (existing is not null)
        {
            throw new AvanciraException($"Country with code '{code}' already exists.");
        }

        var country = Country.Create(
            code,
            dto.Name,
            dto.CurrencyCode,
            dto.DialingCode,
            dto.IsActive);

        await _countryRepository.AddAsync(country, cancellationToken);
        return country.Adapt<CountryDto>();
    }
    public async Task<CountryDto> UpdateAsync(CountryUpdateDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var code = dto.Code.ToUpperInvariant();
        var spec = new CountryByCodeSpec(code);
        var country = await _countryRepository.FirstOrDefaultAsync(spec, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Country '{dto.Code}' not found.");

        country.Update(dto.Name, dto.CurrencyCode, dto.DialingCode, dto.IsActive);
        await _countryRepository.UpdateAsync(country, cancellationToken);

        return country.Adapt<CountryDto>();
    }
    public async Task DeleteAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var spec = new CountryByCodeSpec(code);
        var country = await _countryRepository.FirstOrDefaultAsync(spec, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Country '{code}' not found.");

        await _countryRepository.DeleteAsync(country, cancellationToken);
    }
}
