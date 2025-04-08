namespace Avancira.Application.Identity.Tokens.Dtos;
public record TokenResponse(string Token, string RefreshToken, DateTime RefreshTokenExpiryTime);

