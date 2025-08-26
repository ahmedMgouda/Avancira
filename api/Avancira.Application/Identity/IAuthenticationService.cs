namespace Avancira.Application.Identity;

public interface IAuthenticationService
{
    Task<TokenPair> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri);
    Task<TokenPair> GenerateTokenAsync(string userId);
}

