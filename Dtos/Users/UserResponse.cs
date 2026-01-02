namespace PDVNow.Dtos.Users;

public sealed record UserResponse(
    Guid Id,
    string Username,
    string? Email,
    string UserType,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
