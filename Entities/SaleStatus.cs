using System.ComponentModel;

namespace PDVNow.Entities;

public enum SaleStatus
{
    [Description("Rascunho")]
    Draft = 1,

    [Description("Aguardando pagamento")]
    PendingPayment = 2,

    [Description("Paga")]
    Paid = 3,

    [Description("Cancelada")]
    Cancelled = 4
}
