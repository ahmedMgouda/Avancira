namespace Avancira.Application.UserSessions.Dtos;
public sealed record CreateUserSessionDto(
    string UserId,
    Guid AuthorizationId);
