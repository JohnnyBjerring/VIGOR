using VIGOR.Shared.Interfaces.Services;
using VIGOR.Shared.Models;

namespace VIGOR.MAUI.Services
{
    /// <summary>
    /// Resolver der mapper en rolle til en startside-route baseret på rolle-hierarki.
    /// </summary>
    public class StartPageResolver : IStartPageResolver
    {
        public string ResolveStartRoute(Role role)
        {
            return role.Name switch
            {
                "Superbruger" => "/admin",
                "Leder" => "/admin",
                "Vagtansvarlig" => "/shift/select",
                "Personale" => "/shift/select",
                _ => "/home"
            };
        }
    }
}
