using Microsoft.AspNetCore.Identity;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Interfaces.Services;
using VIGOR.Shared.Models;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// Gateway der wrapper ASP.NET Identity framework-kald.
    /// Implementerer IIdentityGateway – kun denne klasse rører Identity.
    /// Matcher DCD: IdentityGateway.
    /// Følger sekvensdiagrammet: PasswordSignIn → FindByEmail → GetRoles.
    /// </summary>
    public class IdentityGateway : IIdentityGateway
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public IdentityGateway(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public async Task<AuthResult> AuthenticateAsync(string email, string password)
        {
            // 1. FindByEmailAsync
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return new AuthResult
                {
                    Status = AuthStatus.Rejected,
                    Message = "Forkert email eller adgangskode"
                };
            }

            // 2. PasswordSignInAsync (CheckPassword – Blazor kan ikke sætte cookies)
            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
            {
                return new AuthResult
                {
                    Status = AuthStatus.Rejected,
                    Message = "For mange mislykkede forsøg. Prøv igen om 5 minutter."
                };
            }

            if (!signInResult.Succeeded)
            {
                return new AuthResult
                {
                    Status = AuthStatus.Rejected,
                    Message = "Forkert email eller adgangskode"
                };
            }

            // 3. GetRolesAsync
            var roles = await _userManager.GetRolesAsync(user);
            if (roles == null || roles.Count == 0)
            {
                return new AuthResult
                {
                    Status = AuthStatus.Denied,
                    Message = "Du har ikke adgang. Kontakt administrator."
                };
            }

            // Én rolle pr. bruger
            var roleName = roles.First();
            var role = MapToRole(roleName);

            return new AuthResult
            {
                Status = AuthStatus.Success,
                Message = "Login lykkedes",
                UserId = user.Id,
                Role = role
            };
        }

        private static Role MapToRole(string roleName)
        {
            var roleLevels = new Dictionary<string, int>
            {
                { "Leder", 3 },
                { "Vagtansvarlig", 2 },
                { "Personale", 1 }
            };

            return new Role
            {
                Name = roleName,
                Level = roleLevels.GetValueOrDefault(roleName, 0)
            };
        }
    }
}
