using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Client-facing API for UC05. Klienten sender PN-data og aktiv vagttype; serveren udleder afdeling og bruger fra auth-context.
    /// </summary>
    public interface IPnMedicationApi
    {
        Task<IReadOnlyList<PnMedicationDto>?> GetPnMedicationsForCitizenAsync(
            int citizenId,
            CancellationToken cancellationToken = default);

        Task<PnMedicationDto?> RegisterPnMedicationAsync(
            int citizenId,
            RegisterPnMedicationRequest request,
            CancellationToken cancellationToken = default);
    }
}
