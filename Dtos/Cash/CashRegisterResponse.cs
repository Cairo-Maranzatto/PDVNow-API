namespace PDVNow.Dtos.Cash;

public sealed record CashRegisterResponse(
    Guid Id,
    int Code,
    string Name,
    string? Location,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
