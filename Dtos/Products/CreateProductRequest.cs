namespace PDVNow.Dtos.Products;

public sealed record CreateProductRequest(
    string Name,
    string? Description,
    string? Sku,
    string? Barcode,
    string Unit,
    decimal CostPrice,
    decimal SalePrice,
    decimal StockQuantity,
    decimal? MinStockQuantity,
    Guid? SupplierId);
