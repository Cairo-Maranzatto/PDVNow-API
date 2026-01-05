namespace PDVNow.Entities;

public sealed class CashSession
{
    public Guid Id { get; set; }

    public Guid CashRegisterId { get; set; }
    public CashRegister? CashRegister { get; set; }

    public Guid OpenedByUserId { get; set; }
    public Guid? ClosedByUserId { get; set; }

    public DateTimeOffset OpenedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAtUtc { get; set; }

    public decimal OpeningFloatAmount { get; set; }

    public decimal? ClosingCountedAmount { get; set; }

    public string? ClosingNotes { get; set; }
}
