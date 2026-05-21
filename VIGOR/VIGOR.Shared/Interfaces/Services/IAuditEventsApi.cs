using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Client-facing API for UC06 audit-log/historikvisning.
    /// Klienten kan kun læse audit-events; audit-events oprettes server-side af runtime-paths.
    /// </summary>
    public interface IAuditEventsApi
    {
        Task<IReadOnlyList<AuditEventDto>?> GetAuditEventsForCitizenAsync(
            int citizenId,
            CancellationToken cancellationToken = default);
    }
}
