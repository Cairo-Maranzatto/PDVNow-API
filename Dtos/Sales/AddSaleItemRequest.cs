namespace PDVNow.Dtos.Sales;

public sealed record AddSaleItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal? UnitPriceFinal,
    decimal? DiscountAmount,
    string? OverrideCode);
