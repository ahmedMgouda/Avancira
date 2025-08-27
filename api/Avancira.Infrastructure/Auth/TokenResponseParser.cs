using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avancira.Application.Auth.Jwt;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Domain.Common.Exceptions;
using Microsoft.Extensions.Options;

namespace Avancira.Infrastructure.Auth;

public class TokenResponseParser : ITokenResponseParser
{
    private readonly JwtOptions _jwtOptions;

    public TokenResponseParser(IOptions<JwtOptions> jwtOptions)
        => _jwtOptions = jwtOptions.Value;

    public async Task<TokenPair> ParseAsync(Stream stream)
    {
        InternalTokenResponse? response;

        try
        {
            response = await JsonSerializer.DeserializeAsync<InternalTokenResponse>(stream);
        }
        catch (JsonException)
        {
            throw new TokenResponseParseException("Failed to parse token response JSON.");
        }

        if (response is null)
        {
            throw new TokenResponseParseException("Token response is null.");
        }

        if (string.IsNullOrWhiteSpace(response.AccessToken))
        {
            throw new TokenResponseParseException("Access token is missing in the token response.");
        }

        if (string.IsNullOrWhiteSpace(response.RefreshToken))
        {
            throw new TokenResponseParseException("Refresh token is missing in the token response.");
        }

        var token = response.AccessToken;
        var refresh = response.RefreshToken;

        var now = DateTime.UtcNow;

        DateTime refreshExpiry;

        if (response.RefreshTokenExpiresIn is int expiresIn)
        {
            if (expiresIn <= 0)
            {
                throw new TokenResponseParseException(
                    "Refresh token expires in must be positive.");
            }

            var maxSeconds = (long)_jwtOptions.RefreshTokenExpirationInDays * 24 * 60 * 60;
            if (expiresIn > maxSeconds)
            {
                throw new TokenResponseParseException(
                    "Refresh token expires in value is too large.");
            }

            try
            {
                refreshExpiry = now.AddSeconds(expiresIn);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new TokenResponseParseException(
                    "Refresh token expiration calculation overflowed.");
            }
        }
        else
        {
            refreshExpiry = now.AddDays(_jwtOptions.RefreshTokenDefaultDays);
        }

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

