using PDVNow.Auth;

namespace PDVNow.Dtos.Users;

public sealed record UpdateUserRequest(
    string? Email,
    UserType UserType,
    bool IsActive);
