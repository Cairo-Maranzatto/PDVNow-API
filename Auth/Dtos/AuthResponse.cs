namespace PDVNow.Auth.Dtos;

public sealed record AuthResponse(
    Guid UserId,
    string Username,
    string UserType,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
