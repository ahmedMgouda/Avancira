using Avancira.Application.StudentReviews.Dtos;

namespace Avancira.Application.StudentReviews;

public interface IStudentReviewService
{
    Task<StudentReviewDto> GetByLessonIdAsync(int lessonId, CancellationToken cancellationToken = default);
    Task<StudentReviewDto> CreateAsync(StudentReviewCreateDto request, CancellationToken cancellationToken = default);
    Task<StudentReviewDto> RespondAsync(StudentReviewResponseDto request, CancellationToken cancellationToken = default);
}
