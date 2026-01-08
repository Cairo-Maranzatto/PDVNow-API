namespace PDVNow.Dtos.Sales;

public sealed record SaleDetailsResponse(
    SaleResponse Sale,
    IReadOnlyList<SaleItemResponse> Items,
    IReadOnlyList<SalePaymentResponse> Payments,
    SaleBalanceResponse Balance);
