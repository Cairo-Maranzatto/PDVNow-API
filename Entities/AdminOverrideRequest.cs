namespace PDVNow.Entities;

public sealed class AdminOverrideRequest
{
    public Guid Id { get; set; }

    public Guid RequestedByUserId { get; set; }

    public AdminOverridePurpose Purpose { get; set; }

    public Guid? CashRegisterId { get; set; }

    public Guid? CashSessionId { get; set; }

    public Guid? CashMovementId { get; set; }

    public string? Justification { get; set; }

    public DateTimeOffset RequestedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ApprovedAtUtc { get; set; }

    public Guid? ApprovedByAdminUserId { get; set; }

    public Guid? IssuedAdminOverrideCodeId { get; set; }

    public string? RejectionReason { get; set; }
}
