using System;
using Ardalis.Specification;
using Avancira.Domain.UserSessions;

namespace Avancira.Application.UserSessions;

public sealed class ActiveUserSessionsSpec : Specification<UserSession>
{
    public ActiveUserSessionsSpec(string userId)
    {
        Query.Where(s => s.UserId == userId
                          && s.Status == SessionStatus.Active
                          && (!s.TokenExpiresAt.HasValue || s.TokenExpiresAt > DateTimeOffset.UtcNow));
    }
}
