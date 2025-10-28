using Ardalis.Specification;
using Avancira.Domain.UserSessions;

namespace Avancira.Application.UserSessions;

/// <summary>
/// Specification for retrieving sessions older than a given date.
/// Used for cleanup jobs.
/// </summary>
public class ExpiredSessionsSpec : Specification<UserSession>
{
    public ExpiredSessionsSpec(DateTimeOffset beforeDate)
    {
        Query.Where(s => s.CreatedAt < beforeDate);
    }
}
