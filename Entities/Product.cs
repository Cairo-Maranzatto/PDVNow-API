using System.ComponentModel.DataAnnotations;

namespace PDVNow.Entities;

public sealed class Product
{
    public Guid Id { get; set; }

    public int Code { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(60)]
    public string? Sku { get; set; }

    [MaxLength(30)]
    public string? Barcode { get; set; }

    public ProductUnit Unit { get; set; } = ProductUnit.UN;

    public decimal CostPrice { get; set; }

    public decimal SalePrice { get; set; }

    public decimal StockQuantity { get; set; }

    public decimal? MinStockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public bool Excluded { get; set; }

    public Guid? SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
