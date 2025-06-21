using Avancira.Application.Catalog.Dtos;
using System.Threading.Tasks;

namespace Avancira.Application.Lessons;

public interface ILessonService
{
    // Create
    Task<LessonDto> ProposeLessonAsync(LessonDto lessonDto, string userId);

    // Read
    Task<PagedResult<LessonDto>> GetLessonsAsync(string contactId, string userId, int listingId, int page, int pageSize);
    Task<PagedResult<LessonDto>> GetAllLessonsAsync(string userId, int page, int pageSize);
    Task<PagedResult<LessonDto>> GetAllLessonsAsync(string userId, LessonFilter filters);

    // Update
    Task<LessonDto> UpdateLessonStatusAsync(int lessonId, bool accept, string userId);
    Task ProcessPastBookedLessons();
}
