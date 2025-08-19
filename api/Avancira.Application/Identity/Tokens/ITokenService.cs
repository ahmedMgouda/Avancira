using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens.Dtos;
using System;
using System.Collections.Generic;

namespace Avancira.Application.Identity.Tokens;

public interface ITokenService
{
    Task<TokenPair> GenerateTokenAsync(TokenGenerationDto request, ClientInfo clientInfo, CancellationToken cancellationToken);
    Task<TokenPair> RefreshTokenAsync(string? token, string refreshToken, ClientInfo clientInfo, CancellationToken cancellationToken);
    Task RevokeTokenAsync(string refreshToken, string userId, ClientInfo clientInfo, CancellationToken cancellationToken);
    Task<IReadOnlyList<SessionDto>> GetSessionsAsync(string userId, CancellationToken ct);
    Task RevokeSessionAsync(Guid sessionId, string userId, CancellationToken ct);
    Task UpdateSessionActivityAsync(string userId, string deviceId, CancellationToken ct);
}
