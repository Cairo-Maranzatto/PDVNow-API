using System.Security.Cryptography;
using System.Text;

namespace PDVNow.Auth.Services;

public static class TokenHasher
{
    public static string Sha256Base64(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
