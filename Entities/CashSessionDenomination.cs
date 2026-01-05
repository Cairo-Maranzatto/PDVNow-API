namespace PDVNow.Entities;

public sealed class CashSessionDenomination
{
    public Guid Id { get; set; }

    public Guid CashSessionId { get; set; }
    public CashSession? CashSession { get; set; }

    public decimal Denomination { get; set; }

    public int Quantity { get; set; }
}
