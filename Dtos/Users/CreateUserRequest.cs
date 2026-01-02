using PDVNow.Auth;

namespace PDVNow.Dtos.Users;

public sealed record CreateUserRequest(
    string Username,
    string Password,
    string? Email,
    UserType UserType,
    bool IsActive = true);
