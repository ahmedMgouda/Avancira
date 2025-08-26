using System.Security.Cryptography;
using System.Text;

namespace Avancira.Infrastructure.Auth;

public static class TokenUtilities
{
    public static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}

