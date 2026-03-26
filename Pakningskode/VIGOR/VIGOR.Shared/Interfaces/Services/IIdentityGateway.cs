using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Gateway-abstraktion over ASP.NET Identity framework.
    /// AuthService afhænger af denne – ikke direkte af SignInManager/UserManager.
    /// Matcher DCD: IIdentityGateway.
    /// Returnerer kun domæne-typer (AuthResult) – ingen Identity-typer lækker ud.
    /// </summary>
    public interface IIdentityGateway
    {
        Task<AuthResult> AuthenticateAsync(string email, string password);
    }
}
