namespace PDVNow.Dtos.Sales;

public sealed record SaleItemResponse(
    Guid Id,
    Guid ProductId,
    decimal Quantity,
    decimal UnitPriceOriginal,
    decimal UnitPriceFinal,
    decimal DiscountAmount,
    decimal LineTotalAmount,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    Guid? UpdatedByUserId,
    DateTimeOffset? UpdatedAtUtc);
