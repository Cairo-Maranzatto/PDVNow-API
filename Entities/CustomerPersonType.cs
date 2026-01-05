namespace PDVNow.Entities;

using System.ComponentModel;

public enum CustomerPersonType
{
    [Description("Pessoa Física")]
    Individual = 1,

    [Description("Pessoa Jurídica")]
    Company = 2
}
