using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avancira.Application.Auth.Jwt;
using Avancira.Application.Identity.Tokens.Dtos;
using Microsoft.Extensions.Options;

namespace Avancira.Infrastructure.Auth;

public class TokenResponseParser : ITokenResponseParser
{
    private readonly JwtOptions _jwtOptions;

    public TokenResponseParser(IOptions<JwtOptions> jwtOptions)
        => _jwtOptions = jwtOptions.Value;

    public async Task<TokenPair> ParseAsync(Stream stream)
    {
        var response = await JsonSerializer.DeserializeAsync<InternalTokenResponse>(stream);

        var token = response?.AccessToken ?? string.Empty;
        var refresh = response?.RefreshToken ?? string.Empty;

        var refreshExpiry = response?.RefreshTokenExpiresIn is int expiresIn
            ? DateTime.UtcNow.AddSeconds(expiresIn)
            : DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDefaultDays);

        return new TokenPair(token, refresh, refreshExpiry);
    }

    private class InternalTokenResponse
    {
        [JsonPropertyName(AuthConstants.Parameters.AccessToken)]
        public string? AccessToken { get; set; }

        [JsonPropertyName(AuthConstants.Parameters.RefreshToken)]
        public string? RefreshToken { get; set; }

        [JsonPropertyName(AuthConstants.Parameters.RefreshTokenExpiresIn)]
        public int? RefreshTokenExpiresIn { get; set; }
    }
}

