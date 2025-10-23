using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.Listings.Dtos;

namespace Avancira.Application.Listings;

public interface IListingService
{
    Task<IReadOnlyCollection<ListingDto>> GetByTutorIdAsync(string tutorId, CancellationToken cancellationToken = default);
    Task<ListingDto> CreateAsync(ListingCreateDto request, CancellationToken cancellationToken = default);
    Task<ListingDto> UpdateAsync(ListingUpdateDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
