namespace PDVNow.Auth.Dtos;

public record AuthResponseDto(
    Guid UserId,
    string Username,
    UserType UserType);