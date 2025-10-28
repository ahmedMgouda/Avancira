namespace Avancira.Application.Identity.Tokens.Dtos;

/// <summary>
/// Internal DTO representing both access and refresh tokens.
/// The refresh token is never returned to the client but is used by the
/// API to manage the HttpOnly cookie.
/// </summary>
public record TokenPair(string Token, string RefreshToken, DateTime RefreshTokenExpiryTime);
