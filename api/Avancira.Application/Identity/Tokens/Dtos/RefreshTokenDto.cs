namespace Avancira.Application.Identity.Tokens.Dtos;

/// <summary>
/// Request payload for refreshing an access token. The (potentially expired)
/// access token may be provided by the client, but it can be omitted if not
/// available. The refresh token is supplied via a secure HttpOnly cookie.
/// </summary>
public record RefreshTokenDto(string? Token);
