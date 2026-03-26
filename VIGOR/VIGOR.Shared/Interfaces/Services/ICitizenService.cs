using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VIGOR.Shared.Models;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Service-abstraktion for use-casen: hent borgere for en afdeling.
    /// Placeret i Shared så både Web og klient kan afhænge af interfacet.
    /// </summary>
    public interface ICitizenService
    {
        Task<IEnumerable<Citizen>> GetCitizensByDepartmentAsync(int departmentId, CancellationToken cancellationToken = default);
    }
}
