using Avancira.Application.Identity.Tokens.Dtos;

namespace Avancira.Application.Identity.Tokens;
public interface ITokenService
{
    Task<TokenResponse> GenerateTokenAsync(TokenGenerationDto request, string ipAddress, CancellationToken cancellationToken);
    Task<TokenResponse> RefreshTokenAsync(RefreshTokenDto request, string ipAddress, CancellationToken cancellationToken);

}