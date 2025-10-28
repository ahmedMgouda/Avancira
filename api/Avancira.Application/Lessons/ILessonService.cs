using Avancira.Application.Lessons.Dtos;

namespace Avancira.Application.Lessons;

public interface ILessonService
{
    Task<LessonDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<LessonDto> CreateAsync(LessonCreateDto request, CancellationToken cancellationToken = default);
    Task<LessonDto> ConfirmAsync(int lessonId, CancellationToken cancellationToken = default);
    Task<LessonDto> DeclineAsync(LessonDeclineDto request, CancellationToken cancellationToken = default);
    Task<LessonDto> StartAsync(int lessonId, CancellationToken cancellationToken = default);
    Task<LessonDto> CompleteAsync(LessonCompleteDto request, CancellationToken cancellationToken = default);
    Task<LessonDto> CancelAsync(LessonCancelDto request, CancellationToken cancellationToken = default);
    Task<LessonDto> RescheduleAsync(LessonRescheduleDto request, CancellationToken cancellationToken = default);
}
