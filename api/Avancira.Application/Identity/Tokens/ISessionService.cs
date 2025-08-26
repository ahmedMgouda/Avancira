using Avancira.Application.Identity.Tokens.Dtos;

namespace Avancira.Application.Identity.Tokens;

public interface ISessionService
{
    Task<List<SessionDto>> GetActiveSessionsAsync(string userId);
}
