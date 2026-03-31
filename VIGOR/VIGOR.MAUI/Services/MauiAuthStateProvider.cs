using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Maui.Storage;
using VIGOR.Shared.Interfaces.Services;
using VIGOR.Shared.Models;

namespace VIGOR.MAUI.Services
{
    public class MauiAuthStateProvider : AuthenticationStateProvider, IAuthStateService
    {
        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var token = await SecureStorage.Default.GetAsync("jwt_token");

                if (!string.IsNullOrEmpty(token))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);
                    
                    if (jwtToken.ValidTo > DateTime.UtcNow)
                    {
                        var identity = new ClaimsIdentity(jwtToken.Claims, "jwt", ClaimTypes.Email, ClaimTypes.Role);
                        _currentUser = new ClaimsPrincipal(identity);
                    }
                    else
                    {
                        SecureStorage.Default.Remove("jwt_token");
                        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                    }
                }
            }
            catch
            {
                // Ignorer fejl ved læsning af token
            }

            return new AuthenticationState(_currentUser);
        }

        public async Task MarkUserAsAuthenticated(string email, Role role)
        {
            var token = await SecureStorage.Default.GetAsync("jwt_token");
            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var identity = new ClaimsIdentity(jwtToken.Claims, "jwt", ClaimTypes.Email, ClaimTypes.Role);
                _currentUser = new ClaimsPrincipal(identity);
            }
            else
            {
                // Fallback (selvom token bør være gemt før denne kaldes)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, role.Name)
                };
                var identity = new ClaimsIdentity(claims, "MauiAuth");
                _currentUser = new ClaimsPrincipal(identity);
            }
            
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        public async Task MarkUserAsLoggedOut()
        {
            SecureStorage.Default.Remove("jwt_token");
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }
    }
}
