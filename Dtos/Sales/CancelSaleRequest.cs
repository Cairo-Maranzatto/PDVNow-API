namespace PDVNow.Dtos.Sales;

public sealed record CancelSaleRequest(
    string Reason,
    string? OverrideCode);
