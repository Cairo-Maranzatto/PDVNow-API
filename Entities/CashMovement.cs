namespace PDVNow.Entities;

public sealed class CashMovement
{
    public Guid Id { get; set; }

    public Guid CashSessionId { get; set; }
    public CashSession? CashSession { get; set; }

    public CashMovementType Type { get; set; }

    public decimal Amount { get; set; }

    public string? Notes { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guid? AdminOverrideCodeId { get; set; }
    public AdminOverrideCode? AdminOverrideCode { get; set; }
}
