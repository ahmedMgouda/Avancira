using System.IO;
using System.Text.Json;
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
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        var token = root.GetProperty(AuthConstants.Parameters.AccessToken).GetString() ?? string.Empty;
        var refresh = root.GetProperty(AuthConstants.Parameters.RefreshToken).GetString() ?? string.Empty;

        DateTime refreshExpiry;
        if (root.TryGetProperty(AuthConstants.Parameters.RefreshTokenExpiresIn, out var exp))
        {
            refreshExpiry = DateTime.UtcNow.AddSeconds(exp.GetInt32());
        }
        else
        {
            refreshExpiry = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDefaultDays);
        }

        return new TokenPair(token, refresh, refreshExpiry);
    }
}

