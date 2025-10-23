using Avancira.Application.Countries.Dtos;

namespace Avancira.Application.Countries;

public interface ICountryService
{
    Task<IReadOnlyCollection<CountryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CountryDto> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<CountryDto> CreateAsync(CountryCreateDto dto, CancellationToken cancellationToken = default);
    Task<CountryDto> UpdateAsync(CountryUpdateDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(string code, CancellationToken cancellationToken = default);
}
