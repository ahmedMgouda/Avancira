using Ardalis.Specification;
using Avancira.Domain.UserSessions;

namespace Avancira.Application.UserSessions;

/// <summary>
/// Specification for querying all active user sessions by user ID.
/// </summary>
public class ActiveUserSessionsSpec : Specification<UserSession>
{
    public ActiveUserSessionsSpec(string userId)
    {
        Query.Where(s => s.UserId == userId && s.Status == SessionStatus.Active);
    }
}
