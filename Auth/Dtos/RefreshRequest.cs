namespace PDVNow.Auth.Dtos;

public sealed record RefreshRequest(Guid UserId, string RefreshToken);
