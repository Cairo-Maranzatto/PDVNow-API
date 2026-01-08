namespace PDVNow.Entities;

public sealed class SaleItem
{
    public Guid Id { get; set; }

    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPriceOriginal { get; set; }

    public decimal UnitPriceFinal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal LineTotalAmount { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guid? UpdatedByUserId { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
