using VIGOR.Shared.DTOs;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// Server-intern UC05 service. Controller udleder afdeling og bruger-id fra auth-context.
    /// </summary>
    public interface IPnMedicationService
    {
        Task<IReadOnlyList<PnMedicationDto>?> GetForCitizenAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken = default);

        Task<PnMedicationDto?> RegisterAsync(
            int citizenId,
            int departmentId,
            string givenByUserId,
            RegisterPnMedicationRequest request,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null);
    }
}
