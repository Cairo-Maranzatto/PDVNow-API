namespace PDVNow.Auth;

public sealed class CashRegisterOptions
{
    public int OverrideCodeExpirationSeconds { get; set; } = 120;

    public bool RequireOverrideForSupply { get; set; } = false;

    public bool RequireOverrideForWithdrawal { get; set; } = false;
}
