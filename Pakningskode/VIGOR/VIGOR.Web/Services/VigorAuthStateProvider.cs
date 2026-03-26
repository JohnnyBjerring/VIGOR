using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using VIGOR.Shared.Interfaces.Services;
using VIGOR.Shared.Models;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// Custom AuthenticationStateProvider til Blazor Interactive Server.
    /// Implementerer også IAuthStateService for DI-clean adgang fra AuthController.
    /// </summary>
    public class VigorAuthStateProvider : AuthenticationStateProvider, IAuthStateService
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
        private bool _initialized;

        public VigorAuthStateProvider(ProtectedSessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (!_initialized)
            {
                _initialized = true;
                try
                {
                    var result = await _sessionStorage.GetAsync<AuthSessionData>("vigor_auth");
                    if (result.Success && result.Value != null)
                    {
                        SetClaimsPrincipal(result.Value.Email, result.Value.RoleName);
                    }
                }
                catch
                {
                    // Under prerendering – JS interop ikke tilgængelig endnu
                }
            }

            return new AuthenticationState(_currentUser);
        }

        /// <summary>
        /// Kaldt efter succesful login – gemmer i sessionStorage og sætter claims.
        /// </summary>
        public async Task MarkUserAsAuthenticated(string email, Role role)
        {
            var sessionData = new AuthSessionData
            {
                Email = email,
                RoleName = role.Name
            };

            await _sessionStorage.SetAsync("vigor_auth", sessionData);
            SetClaimsPrincipal(email, role.Name);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        /// <summary>
        /// Kaldt ved logout – rydder sessionStorage og claims.
        /// </summary>
        public async Task MarkUserAsLoggedOut()
        {
            await _sessionStorage.DeleteAsync("vigor_auth");
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            _initialized = true;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private void SetClaimsPrincipal(string email, string roleName)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, email),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Role, roleName)
            };

            _currentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "VigorAuth"));
        }
    }

    /// <summary>
    /// Simpel DTO til sessionStorage – kun email og rollenavn.
    /// </summary>
    public class AuthSessionData
    {
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }
}
