namespace PDVNow.Entities;

public sealed class AdminOverrideCode
{
    public Guid Id { get; set; }

    public string CodeHash { get; set; } = string.Empty;

    public AdminOverridePurpose Purpose { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? UsedAtUtc { get; set; }

    public Guid CreatedByAdminUserId { get; set; }

    public Guid? UsedByUserId { get; set; }

    public Guid? CashRegisterId { get; set; }

    public Guid? CashSessionId { get; set; }

    public Guid? CashMovementId { get; set; }

    public string? Justification { get; set; }
}
