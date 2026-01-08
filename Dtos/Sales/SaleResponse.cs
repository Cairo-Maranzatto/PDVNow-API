using PDVNow.Entities;

namespace PDVNow.Dtos.Sales;

public sealed record SaleResponse(
    Guid Id,
    int Code,
    SaleStatus Status,
    Guid CashRegisterId,
    Guid CashSessionId,
    Guid CustomerId,
    decimal SubtotalAmount,
    decimal ItemDiscountTotalAmount,
    decimal SaleDiscountAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    Guid? UpdatedByUserId,
    DateTimeOffset? UpdatedAtUtc,
    Guid? FinalizedByUserId,
    DateTimeOffset? FinalizedAtUtc,
    Guid? CancelledByUserId,
    DateTimeOffset? CancelledAtUtc,
    string? CancelReason);
