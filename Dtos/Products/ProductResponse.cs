namespace PDVNow.Dtos.Products;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Sku,
    string? Barcode,
    string Unit,
    decimal CostPrice,
    decimal SalePrice,
    decimal StockQuantity,
    decimal? MinStockQuantity,
    bool IsActive,
    Guid? SupplierId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
