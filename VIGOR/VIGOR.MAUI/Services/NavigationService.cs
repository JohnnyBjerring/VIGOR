using Microsoft.AspNetCore.Components;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.MAUI.Services
{
    /// <summary>
    /// Wrapper over Blazor NavigationManager – implementerer INavigationService.
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly NavigationManager _navigationManager;

        public NavigationService(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        public void Navigate(string route)
        {
            _navigationManager.NavigateTo(route);
        }
    }
}
