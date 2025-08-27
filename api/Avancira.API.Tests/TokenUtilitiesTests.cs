using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Avancira.Infrastructure.Auth;
using FluentAssertions;
using Xunit;

public class TokenUtilitiesTests
{
    [Fact]
    public void HashToken_WithDifferentSalts_ProducesDifferentHashes()
    {
        var token = "token";
        var secret = "secret";
        var salt1 = RandomNumberGenerator.GetBytes(16);
        var salt2 = RandomNumberGenerator.GetBytes(16);

        var hash1 = TokenUtilities.HashToken(token, secret, salt1);
        var hash2 = TokenUtilities.HashToken(token, secret, salt2);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashToken_WithHmac_ProducesExpectedHash()
    {
        var token = "token";
        var secret = "secret";
        var salt = Encoding.UTF8.GetBytes("salt");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(token).Concat(salt).ToArray()));

        TokenUtilities.HashToken(token, secret, salt).Should().Be(expected);
    }
}
