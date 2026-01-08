namespace PDVNow.Entities;

public sealed class Sale
{
    public Guid Id { get; set; }

    public int Code { get; set; }

    public SaleStatus Status { get; set; } = SaleStatus.Draft;

    public Guid CashRegisterId { get; set; }
    public CashRegister? CashRegister { get; set; }

    public Guid CashSessionId { get; set; }
    public CashSession? CashSession { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public decimal SubtotalAmount { get; set; }

    public decimal ItemDiscountTotalAmount { get; set; }

    public decimal SaleDiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guid? UpdatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? FinalizedByUserId { get; set; }
    public DateTimeOffset? FinalizedAtUtc { get; set; }

    public Guid? CancelledByUserId { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }

    public string? CancelReason { get; set; }
}
