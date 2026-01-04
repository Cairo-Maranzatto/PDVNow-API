namespace PDVNow.Auth.Dtos;

public record AuthResponseDto(
    Guid UserId,
    string Username,
    string UserType);