namespace PDVNow.Dtos.Sales;

public sealed record FinalizeSaleRequest(
    decimal? SaleDiscountAmount,
    string? OverrideCode);
