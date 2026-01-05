using System.ComponentModel;

namespace PDVNow.Entities;

public enum AdminOverridePurpose
{
    [Description("Abrir caixa")]
    OpenSession = 1,

    [Description("Fechar caixa")]
    CloseSession = 2,

    [Description("Reabrir caixa")]
    ReopenSession = 3,

    [Description("Movimentação de caixa")]
    CashMovement = 4
}
