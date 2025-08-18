namespace Avancira.Application.Identity.Tokens.Dtos;
public record TokenGenerationDto(string Email, string Password, bool RememberMe);
