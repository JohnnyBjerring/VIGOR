using VIGOR.Shared.DTOs;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// Server-intern UC04 service. Controller udleder afdeling og bruger-id fra auth-context.
    /// </summary>
    public interface IFixedMedicationService
    {
        Task<IReadOnlyList<FixedMedicationDto>?> GetForCitizenAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken = default);

        Task<FixedMedicationDto?> GiveAsync(
            int citizenId,
            int fixedMedicationId,
            int departmentId,
            string givenByUserId,
            DateTime? givenAt,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null);

        Task<FixedMedicationDto?> CancelGivenAsync(
            int citizenId,
            int fixedMedicationId,
            int departmentId,
            string cancelledByUserId,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null);

        Task<FixedMedicationDto?> UpdatePlanAsync(
            int citizenId,
            int fixedMedicationId,
            int departmentId,
            UpdateFixedMedicationPlanRequest request,
            CancellationToken cancellationToken = default);
    }
}
