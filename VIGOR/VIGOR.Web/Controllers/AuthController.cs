using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.Web.Controllers
{
    /// <summary>
    /// AuthController – koordinerer login-flow (UC01).
    /// Afhænger kun af interfaces (DI) – ingen direkte kald til Identity.
    /// Følger sekvensdiagrammet: SignIn → MarkAuthenticated → ResolveStartRoute → Navigate.
    /// </summary>
    public class AuthController
    {
        private readonly IAuthService _authService;
        private readonly IStartPageResolver _resolver;
        private readonly INavigationService _navigation;
        private readonly IAuthStateService _authState;

        public AuthController(
            IAuthService authService,
            IStartPageResolver resolver,
            INavigationService navigation,
            IAuthStateService authState)
        {
            _authService = authService;
            _resolver = resolver;
            _navigation = navigation;
            _authState = authState;
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            var result = await _authService.SignInAsync(email, password);

            if (result.Status == AuthStatus.Success && result.Role != null)
            {
                await _authState.MarkUserAsAuthenticated(email, result.Role);

                var route = _resolver.ResolveStartRoute(result.Role);
                _navigation.Navigate(route);
            }

            return result;
        }

        public async Task LogoutAsync()
        {
            await _authState.MarkUserAsLoggedOut();
            _navigation.Navigate("/login");
        }
    }
}
