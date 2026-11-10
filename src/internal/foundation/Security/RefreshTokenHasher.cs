using System.Security.Cryptography;
using System.Text;

namespace SailNora.Foundation.Security;

public static class RefreshTokenHasher
{
    public static string Hash(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }

    public static string CreateRawToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
