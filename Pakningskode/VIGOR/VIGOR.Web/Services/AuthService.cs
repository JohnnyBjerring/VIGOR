using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// Application service til autentificering (UC01).
    /// Afhænger af IIdentityGateway – ikke direkte af SignInManager/UserManager.
    /// Matcher DCD: AuthService → IIdentityGateway.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IIdentityGateway _identity;

        public AuthService(IIdentityGateway identity)
        {
            _identity = identity;
        }

        public async Task<AuthResult> SignInAsync(string email, string password)
        {
            return await _identity.AuthenticateAsync(email, password);
        }
    }
}
