using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Client-facing API-kontrakt for UC07.
    /// Klienten sender kun valgt vagttype; serveren udleder bruger og afdeling fra auth-context.
    /// </summary>
    public interface IShiftSelectionApi
    {
        Task<ActiveShiftContextDto?> SelectShiftAsync(
            SelectShiftRequest request,
            CancellationToken cancellationToken = default);
    }
}
