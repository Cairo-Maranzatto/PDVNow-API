using System.ComponentModel.DataAnnotations;

namespace PDVNow.Auth.Entities;

public sealed class AppUser
{
    public Guid Id { get; set; }

    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    public UserType UserType { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
