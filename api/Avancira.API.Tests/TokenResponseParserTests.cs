using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avancira.Application.Auth.Jwt;
using Avancira.Domain.Common.Exceptions;
using Avancira.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Xunit;

public class TokenResponseParserTests
{
    [Fact]
    public async Task ParseAsync_MissingAccessToken_ThrowsTokenResponseParseException()
    {
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [AuthConstants.Parameters.RefreshToken] = "refresh",
            [AuthConstants.Parameters.RefreshTokenExpiresIn] = 3600
        });

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var parser = new TokenResponseParser(Options.Create(new JwtOptions()));

        await Assert.ThrowsAsync<TokenResponseParseException>(() => parser.ParseAsync(stream));
    }

    [Fact]
    public async Task ParseAsync_MissingRefreshToken_ThrowsTokenResponseParseException()
    {
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [AuthConstants.Parameters.AccessToken] = "token",
            [AuthConstants.Parameters.RefreshTokenExpiresIn] = 3600
        });

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var parser = new TokenResponseParser(Options.Create(new JwtOptions()));

        await Assert.ThrowsAsync<TokenResponseParseException>(() => parser.ParseAsync(stream));
    }

    [Fact]
    public async Task ParseAsync_MalformedJson_ThrowsTokenResponseParseException()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{bad json"));
        var parser = new TokenResponseParser(Options.Create(new JwtOptions()));

        await Assert.ThrowsAsync<TokenResponseParseException>(() => parser.ParseAsync(stream));
    }
}
