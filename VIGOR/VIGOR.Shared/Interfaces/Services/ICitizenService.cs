using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VIGOR.Shared.Models;
using VIGOR.Shared.Enums;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Service-abstraktion for use-casen: hent borgere for en afdeling.
    /// Placeret i Shared så både Web og klient kan afhænge af interfacet.
    /// </summary>
    public interface ICitizenService
    {
        Task<IEnumerable<Citizen>> GetCitizensByDepartmentAsync(int departmentId, CancellationToken cancellationToken = default);
        Task<Citizen?> UpdateCitizenStatusAsync(int citizenId, int departmentId, CitizenStatus status, CancellationToken cancellationToken = default);
    }
}
