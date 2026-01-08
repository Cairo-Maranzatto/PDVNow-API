using System.ComponentModel;

namespace PDVNow.Entities;

public enum SalePaymentMethod
{
    [Description("Dinheiro")]
    Cash = 1,

    [Description("PIX")]
    Pix = 2,

    [Description("Cartão de Débito")]
    DebitCard = 3,

    [Description("Cartão de Crédito")]
    CreditCard = 4,

    [Description("Voucher/vale")]
    Voucher = 5,

    [Description("Boleto")]
    Boleto = 6,

    [Description("Transferência")]
    BankTransfer = 7,

    [Description("Crediário/fiado")]
    StoreCredit = 8,

    [Description("Outros")]
    Other = 99
}
