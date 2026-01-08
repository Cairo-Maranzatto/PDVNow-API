namespace PDVNow.Dtos.Sales;

public sealed record SaleBalanceResponse(
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount);
