using System;
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
        var salt1 = TokenUtilities.GenerateSalt();
        var salt2 = TokenUtilities.GenerateSalt();

        var hash1 = TokenUtilities.HashToken(token, secret, salt1);
        var hash2 = TokenUtilities.HashToken(token, secret, salt2);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashToken_WithHmac_ProducesExpectedHash()
    {
        var token = "token";
        var secret = "secret";
        var saltBytes = Encoding.UTF8.GetBytes("salt");
        var salt = Convert.ToBase64String(saltBytes);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(token).Concat(saltBytes).ToArray()));

        TokenUtilities.HashToken(token, secret, salt).Should().Be(expected);
    }
}
