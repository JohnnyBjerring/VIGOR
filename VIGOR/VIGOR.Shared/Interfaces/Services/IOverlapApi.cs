using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Client-facing API til UC08 overlapvisning.
    /// Serveren udleder afdeling/adgang fra auth-context; klienten sender kun aktiv vagttype som visningskontekst.
    /// </summary>
    public interface IOverlapApi
    {
        Task<OverlapDto?> GetOverlapAsync(
            ShiftType? activeShiftType = null,
            CancellationToken cancellationToken = default);
    }
}
