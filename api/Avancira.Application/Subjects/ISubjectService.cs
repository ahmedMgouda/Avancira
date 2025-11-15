using Avancira.Application.Subjects.Dtos;

namespace Avancira.Application.Subjects;

public interface ISubjectService
{
    /// <summary>
    /// Get a subject by its ID
    /// </summary>
    Task<SubjectDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all subjects, optionally filtered by category
    /// </summary>
    /// <param name="categoryId">Optional category filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of subjects sorted by SortOrder then Name</returns>
    Task<IEnumerable<SubjectDto>> GetAllAsync(
        int? categoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new subject with optional icon upload
    /// </summary>
    /// <param name="request">Subject creation data including optional icon file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<SubjectDto> CreateAsync(
        SubjectCreateDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing subject with optional icon replacement
    /// </summary>
    /// <param name="request">Subject update data including optional new icon file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<SubjectDto> UpdateAsync(
        SubjectUpdateDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a subject and its associated icon file
    /// </summary>
    /// <param name="id">Subject ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}