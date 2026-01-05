namespace PDVNow.Dtos.Cash;

public sealed record CashSessionResponse(
    Guid Id,
    Guid CashRegisterId,
    Guid OpenedByUserId,
    Guid? ClosedByUserId,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    decimal OpeningFloatAmount,
    decimal? ClosingCountedAmount,
    string? ClosingNotes);
