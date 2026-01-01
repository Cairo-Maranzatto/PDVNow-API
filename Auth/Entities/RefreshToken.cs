using System.ComponentModel.DataAnnotations;

namespace PDVNow.Auth.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public AppUser? User { get; set; }

    [MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public bool IsRevoked => RevokedAtUtc != null;

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
}
