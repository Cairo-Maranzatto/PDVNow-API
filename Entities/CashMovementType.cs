using System.ComponentModel;

namespace PDVNow.Entities;

public enum CashMovementType
{
    [Description("Suprimento")]
    Supply = 1,

    [Description("Sangria")]
    Withdrawal = 2
}
