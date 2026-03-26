using VIGOR.Shared.Models;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Interface til auth state management.
    /// AuthController afhænger af denne – ikke direkte af VigorAuthStateProvider.
    /// </summary>
    public interface IAuthStateService
    {
        Task MarkUserAsAuthenticated(string email, Role role);
        Task MarkUserAsLoggedOut();
    }
}
