using PDVNow.Entities;

namespace PDVNow.Dtos.Sales;

public sealed record SalePaymentResponse(
    Guid Id,
    SalePaymentMethod Method,
    decimal Amount,
    decimal? AmountReceived,
    decimal? ChangeGiven,
    string? AuthorizationCode,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc);
