namespace PDVNow.Auth;

public sealed class SeedAdminOptions
{
    public bool Enabled { get; set; } = true;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = string.Empty;
}
