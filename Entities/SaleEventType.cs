using System.ComponentModel;

namespace PDVNow.Entities;

public enum SaleEventType
{
    [Description("Venda criada")]
    Created = 1,

    [Description("Item adicionado")]
    ItemAdded = 2,

    [Description("Item atualizado")]
    ItemUpdated = 3,

    [Description("Item removido")]
    ItemRemoved = 4,

    [Description("Pagamento adicionado")]
    PaymentAdded = 5,

    [Description("Venda finalizada")]
    Finalized = 6,

    [Description("Venda cancelada")]
    Cancelled = 7
}
