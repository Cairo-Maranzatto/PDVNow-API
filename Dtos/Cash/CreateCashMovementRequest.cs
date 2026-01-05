using PDVNow.Entities;

namespace PDVNow.Dtos.Cash;

public sealed record CreateCashMovementRequest(
    Guid CashRegisterId,
    CashMovementType Type,
    decimal Amount,
    string? Notes,
    string? OverrideCode);
