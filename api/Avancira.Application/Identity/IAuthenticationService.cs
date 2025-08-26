using Avancira.Application.Identity.Tokens.Dtos;

namespace Avancira.Application.Identity;

public interface IAuthenticationService
{
    Task<TokenPair> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri);
    Task<TokenPair> GenerateTokenAsync(string userId);
    Task<TokenPair> RefreshTokenAsync(string refreshToken);
    Task<TokenPair?> PasswordSignInAsync(string email, string password);
}

