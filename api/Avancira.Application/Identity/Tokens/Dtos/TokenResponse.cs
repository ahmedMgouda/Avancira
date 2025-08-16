namespace Avancira.Application.Identity.Tokens.Dtos;

/// <summary>
/// Response returned to the client after authentication or token refresh.
/// Contains only the short-lived access token.
/// </summary>
public record TokenResponse(string Token);
