namespace PDVNow.Dtos.Sales;

public sealed record CreateSaleRequest(
    Guid CashRegisterId,
    Guid CustomerId);
