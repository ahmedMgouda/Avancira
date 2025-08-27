using System.Security.Cryptography;
using System.Text;

namespace Avancira.Infrastructure.Auth;

public static class TokenUtilities
{
    public static string HashToken(string token, string secret, byte[]? salt = null)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        if (salt is { Length: > 0 })
        {
            var combined = new byte[tokenBytes.Length + salt.Length];
            Buffer.BlockCopy(tokenBytes, 0, combined, 0, tokenBytes.Length);
            Buffer.BlockCopy(salt, 0, combined, tokenBytes.Length, salt.Length);
            tokenBytes = combined;
        }
        var hash = hmac.ComputeHash(tokenBytes);
        return Convert.ToBase64String(hash);
    }
}
