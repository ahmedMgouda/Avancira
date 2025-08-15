namespace Avancira.Application.Identity.Tokens.Dtos;

/// <summary>
/// Request payload for refreshing an access token. Only the (potentially expired)
/// access token is sent by the client. The refresh token is supplied via a
/// secure HttpOnly cookie.
/// </summary>
public record RefreshTokenDto(string? Token);
