using Avancira.Application.Identity.Tokens.Dtos;

namespace Avancira.Application.Identity;

public interface IAuthenticationService
{
    Task<TokenPair> GenerateTokenAsync(TokenGenerationDto request);
    Task<TokenPair> RefreshTokenAsync(string refreshToken);
}

