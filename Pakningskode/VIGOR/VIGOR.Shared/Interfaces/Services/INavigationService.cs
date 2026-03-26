namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Abstraktion over navigation – wrapper NavigationManager.
    /// </summary>
    public interface INavigationService
    {
        void Navigate(string route);
    }
}
