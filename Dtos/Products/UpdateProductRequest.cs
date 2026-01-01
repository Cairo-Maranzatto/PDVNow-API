namespace PDVNow.Dtos.Products;

public sealed record UpdateProductRequest(
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
    Guid? SupplierId);
