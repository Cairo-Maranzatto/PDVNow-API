namespace PDVNow.Entities;

public sealed class SalePayment
{
    public Guid Id { get; set; }

    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    public SalePaymentMethod Method { get; set; }

    public decimal Amount { get; set; }

    public decimal? AmountReceived { get; set; }

    public decimal? ChangeGiven { get; set; }

    public string? AuthorizationCode { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
