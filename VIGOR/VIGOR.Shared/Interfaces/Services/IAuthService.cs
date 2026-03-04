using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Application service til autentificering (UC01).
    /// Controller afhænger af denne – ikke af Identity direkte.
    /// </summary>
    public interface IAuthService
    {
        Task<AuthResult> SignInAsync(string email, string password);
    }
}
