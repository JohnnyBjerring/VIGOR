using VIGOR.Shared.Models;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Resolver der mapper en rolle til en startside-route.
    /// </summary>
    public interface IStartPageResolver
    {
        string ResolveStartRoute(Role role);
    }
}
