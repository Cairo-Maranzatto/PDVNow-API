namespace PDVNow.Auth;

using System.ComponentModel;

public enum UserType
{
    [Description("Administrador")]
    Admin = 1,

    [Description("Usuário do PDV")]
    PdvUser = 2
}
