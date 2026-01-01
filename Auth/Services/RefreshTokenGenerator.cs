using System.Security.Cryptography;

namespace PDVNow.Auth.Services;

public static class RefreshTokenGenerator
{
    public static string GenerateOpaqueToken(int sizeBytes = 64)
    {
        var bytes = RandomNumberGenerator.GetBytes(sizeBytes);
        return Convert.ToBase64String(bytes);
    }
}
