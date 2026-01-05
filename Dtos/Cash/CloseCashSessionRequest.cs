namespace PDVNow.Dtos.Cash;

public sealed record CloseCashSessionRequest(
    Guid CashRegisterId,
    IReadOnlyList<CashDenominationCountDto> Denominations,
    string? Notes,
    string? OverrideCode);
