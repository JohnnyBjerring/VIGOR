using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC08 server-side service til samlet overlapvisning for den indloggede brugers afdeling.
    /// </summary>
    public interface IOverlapService
    {
        Task<OverlapDto?> GetOverlapAsync(
            int departmentId,
            ShiftType? activeShiftType = null,
            CancellationToken cancellationToken = default);
    }
}
