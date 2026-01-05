namespace PDVNow.Entities;

using System.ComponentModel;

public enum ProductUnit
{
    [Description("Unidade")]
    UN = 1,

    [Description("Quilograma")]
    KG = 2,

    [Description("Grama")]
    G = 3,

    [Description("Miligrama")]
    MG = 4,

    [Description("Litro")]
    L = 5,

    [Description("Mililitro")]
    ML = 6,

    [Description("Metro")]
    M = 7,

    [Description("Centímetro")]
    CM = 8,

    [Description("Milímetro")]
    MM = 9,

    [Description("Metro quadrado")]
    M2 = 10,

    [Description("Metro cúbico")]
    M3 = 11,

    [Description("Caixa")]
    CX = 12,

    [Description("Peça")]
    PC = 13,

    [Description("Pacote")]
    PT = 14,

    [Description("Fardo")]
    FD = 15,

    [Description("Rolo")]
    RL = 16,

    [Description("Par")]
    PR = 17,

    [Description("Saco")]
    SC = 18,

    [Description("Lata")]
    LT = 19
}
