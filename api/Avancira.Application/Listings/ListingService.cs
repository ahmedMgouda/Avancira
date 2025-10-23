using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.Persistence;
using Avancira.Application.Listings.Dtos;
using Avancira.Application.Listings.Specifications;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Subjects;
using Avancira.Domain.Tutors;
using Mapster;

namespace Avancira.Application.Listings;

public class ListingService : IListingService
{
    private readonly IRepository<Listing> _listingRepository;
    private readonly IReadRepository<Subject> _subjectReadRepository;

    public ListingService(
        IRepository<Listing> listingRepository,
        IReadRepository<Subject> subjectReadRepository)
    {
        _listingRepository = listingRepository;
        _subjectReadRepository = subjectReadRepository;
    }

    public async Task<IReadOnlyCollection<ListingDto>> GetByTutorIdAsync(string tutorId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tutorId);

        var spec = new ListingsByTutorSpec(tutorId);
        var listings = await _listingRepository.ListAsync(spec, cancellationToken);
        return listings
            .OrderBy(listing => listing.SortOrder)
            .ThenBy(listing => listing.Subject.Name)
            .Adapt<IReadOnlyCollection<ListingDto>>();
    }

    public async Task<ListingDto> CreateAsync(ListingCreateDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subject = await _subjectReadRepository.GetByIdAsync(request.SubjectId, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Subject '{request.SubjectId}' not found.");

        var existingSpec = new ListingsByTutorSpec(request.TutorId);
        var existingListings = await _listingRepository.ListAsync(existingSpec, cancellationToken);
        if (existingListings.Any(listing => listing.SubjectId == request.SubjectId))
        {
            throw new AvanciraException("You already teach this subject.");
        }

        if (request.HourlyRate <= 0)
        {
            throw new AvanciraException("Hourly rate must be greater than zero.");
        }

        var listing = Listing.Create(
            request.TutorId,
            request.SubjectId,
            request.HourlyRate,
            true,
            request.SortOrder);

        await _listingRepository.AddAsync(listing, cancellationToken);

        listing.Subject = subject;

        return listing.Adapt<ListingDto>();
    }

    public async Task<ListingDto> UpdateAsync(ListingUpdateDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new ListingByIdSpec(request.Id);
        var listing = await _listingRepository.FirstOrDefaultAsync(spec, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Listing '{request.Id}' not found.");

        if (request.HourlyRate <= 0)
        {
            throw new AvanciraException("Hourly rate must be greater than zero.");
        }

        listing.Update(request.HourlyRate, request.IsActive, request.SortOrder);

        await _listingRepository.UpdateAsync(listing, cancellationToken);

        return listing.Adapt<ListingDto>();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var listing = await _listingRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Listing '{id}' not found.");

        await _listingRepository.DeleteAsync(listing, cancellationToken);
    }
}
