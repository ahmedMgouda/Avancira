using System.Linq;
using System.Threading;
using Avancira.Application.Persistence;
using Avancira.Application.TutorSubjects.Dtos;
using Avancira.Application.TutorSubjects.Specifications;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Subjects;
using Avancira.Domain.Tutors;
using Mapster;

namespace Avancira.Application.TutorSubjects;

public class TutorSubjectService : ITutorSubjectService
{
    private readonly IRepository<TutorSubject> _tutorSubjectRepository;
    private readonly IReadRepository<Subject> _subjectReadRepository;

    public TutorSubjectService(
        IRepository<TutorSubject> tutorSubjectRepository,
        IReadRepository<Subject> subjectReadRepository)
    {
        _tutorSubjectRepository = tutorSubjectRepository;
        _subjectReadRepository = subjectReadRepository;
    }

    public async Task<IReadOnlyCollection<TutorSubjectDto>> GetByTutorIdAsync(string tutorId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tutorId);

        var spec = new TutorSubjectsByTutorSpec(tutorId);
        var subjects = await _tutorSubjectRepository.ListAsync(spec, cancellationToken);
        return subjects
            .OrderBy(subject => subject.SortOrder)
            .ThenBy(subject => subject.Subject.Name)
            .Adapt<IReadOnlyCollection<TutorSubjectDto>>();
    }

    public async Task<TutorSubjectDto> CreateAsync(TutorSubjectCreateDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subject = await _subjectReadRepository.GetByIdAsync(request.SubjectId, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Subject '{request.SubjectId}' not found.");

        var existingSpec = new TutorSubjectsByTutorSpec(request.TutorId);
        var existingSubjects = await _tutorSubjectRepository.ListAsync(existingSpec, cancellationToken);
        if (existingSubjects.Any(s => s.SubjectId == request.SubjectId))
        {
            throw new AvanciraException("You already teach this subject.");
        }

        if (request.HourlyRate <= 0)
        {
            throw new AvanciraException("Hourly rate must be greater than zero.");
        }

        var tutorSubject = TutorSubject.Create(
            request.TutorId,
            request.SubjectId,
            request.HourlyRate,
            true,
            request.SortOrder);

        await _tutorSubjectRepository.AddAsync(tutorSubject, cancellationToken);

        tutorSubject.Subject = subject;

        return tutorSubject.Adapt<TutorSubjectDto>();
    }

    public async Task<TutorSubjectDto> UpdateAsync(TutorSubjectUpdateDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new TutorSubjectByIdSpec(request.Id);
        var tutorSubject = await _tutorSubjectRepository.FirstOrDefaultAsync(spec, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Tutor subject '{request.Id}' not found.");

        if (request.HourlyRate <= 0)
        {
            throw new AvanciraException("Hourly rate must be greater than zero.");
        }

        tutorSubject.Update(request.HourlyRate, request.IsActive, request.SortOrder);

        await _tutorSubjectRepository.UpdateAsync(tutorSubject, cancellationToken);

        return tutorSubject.Adapt<TutorSubjectDto>();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var tutorSubject = await _tutorSubjectRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AvanciraNotFoundException($"Tutor subject '{id}' not found.");

        await _tutorSubjectRepository.DeleteAsync(tutorSubject, cancellationToken);
    }
}
