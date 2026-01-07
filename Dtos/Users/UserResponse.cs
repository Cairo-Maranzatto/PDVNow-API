using PDVNow.Auth;

namespace PDVNow.Dtos.Users;

public sealed record UserResponse(
    Guid Id,
    string Username,
    string? Email,
    UserType UserType,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
