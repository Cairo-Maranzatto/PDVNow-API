namespace PDVNow.Auth.Dtos;

public sealed record AuthResponse(
    Guid UserId,
    string Username,
    UserType UserType,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
