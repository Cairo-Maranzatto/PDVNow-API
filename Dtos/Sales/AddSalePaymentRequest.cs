using PDVNow.Entities;

namespace PDVNow.Dtos.Sales;

public sealed record AddSalePaymentRequest(
    SalePaymentMethod Method,
    decimal Amount,
    decimal? AmountReceived,
    string? AuthorizationCode);
