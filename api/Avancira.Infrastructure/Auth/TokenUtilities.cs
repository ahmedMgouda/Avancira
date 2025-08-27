using System;
using System.Security.Cryptography;
using System.Text;

namespace Avancira.Infrastructure.Auth;

public static class TokenUtilities
{
    public static string GenerateSalt(int size = 16)
    {
        var salt = RandomNumberGenerator.GetBytes(size);
        return Convert.ToBase64String(salt);
    }

    public static string HashToken(string token, string secret, string salt)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var saltBytes = Convert.FromBase64String(salt);
        var combined = new byte[tokenBytes.Length + saltBytes.Length];
        Buffer.BlockCopy(tokenBytes, 0, combined, 0, tokenBytes.Length);
        Buffer.BlockCopy(saltBytes, 0, combined, tokenBytes.Length, saltBytes.Length);
        var hash = hmac.ComputeHash(combined);
        return Convert.ToBase64String(hash);
    }
}
