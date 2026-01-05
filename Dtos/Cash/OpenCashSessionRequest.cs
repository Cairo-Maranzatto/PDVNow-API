namespace PDVNow.Dtos.Cash;

public sealed record OpenCashSessionRequest(
    string CashRegisterName,
    string? Location,
    decimal OpeningFloatAmount,
    string? OverrideCode);
