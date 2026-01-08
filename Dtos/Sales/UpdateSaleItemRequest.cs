namespace PDVNow.Dtos.Sales;

public sealed record UpdateSaleItemRequest(
    decimal Quantity,
    decimal? UnitPriceFinal,
    decimal? DiscountAmount,
    string? OverrideCode);
