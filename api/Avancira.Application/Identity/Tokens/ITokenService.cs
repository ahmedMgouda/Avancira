using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens.Dtos;

namespace Avancira.Application.Identity.Tokens;

public interface ITokenService
{
    Task<TokenPair> GenerateTokenAsync(TokenGenerationDto request, ClientInfo clientInfo, CancellationToken cancellationToken);
    Task<TokenPair> RefreshTokenAsync(string? token, string refreshToken, ClientInfo clientInfo, CancellationToken cancellationToken);
    Task RevokeTokenAsync(string refreshToken, string userId, ClientInfo clientInfo, CancellationToken cancellationToken);
}
