using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Client-facing API for UC04. Klienten skal ikke sende afdeling; serveren udleder adgang fra auth-context.
    /// </summary>
    public interface IFixedMedicationApi
    {
        Task<IReadOnlyList<FixedMedicationDto>?> GetFixedMedicationsForCitizenAsync(
            int citizenId,
            CancellationToken cancellationToken = default);

        Task<FixedMedicationDto?> RegisterFixedMedicationGivenAsync(
            int citizenId,
            int fixedMedicationId,
            DateTime? givenAt,
            CancellationToken cancellationToken = default);

        Task<FixedMedicationDto?> CancelFixedMedicationGivenAsync(
            int citizenId,
            int fixedMedicationId,
            CancellationToken cancellationToken = default);

        Task<FixedMedicationDto?> UpdateFixedMedicationPlanAsync(
            int citizenId,
            int fixedMedicationId,
            UpdateFixedMedicationPlanRequest request,
            CancellationToken cancellationToken = default);
    }
}
