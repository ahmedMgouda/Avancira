using System.Security.Cryptography;
using System.Text;

namespace Avancira.Infrastructure.Auth;

public static class TokenUtilities
{
    public static (byte[] Salt, string Hash) HashToken(string token, string secret, byte[]? salt = null)
    {
        salt ??= RandomNumberGenerator.GetBytes(16);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var tokenBytes = Encoding.UTF8.GetBytes(token);

        if (salt.Length > 0)
        {
            var combined = new byte[tokenBytes.Length + salt.Length];
            Buffer.BlockCopy(tokenBytes, 0, combined, 0, tokenBytes.Length);
            Buffer.BlockCopy(salt, 0, combined, tokenBytes.Length, salt.Length);
            tokenBytes = combined;
        }

        var hash = hmac.ComputeHash(tokenBytes);
        return (salt, Convert.ToBase64String(hash));
    }
}
