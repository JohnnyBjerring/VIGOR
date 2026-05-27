using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Client-side API-kontrakt for UC11 personale-tildeling.
    /// </summary>
    public interface IStaffAssignmentApi
    {
        Task<IReadOnlyList<CitizenStaffAssignmentDto>?> GetAssignmentsForCitizenAsync(
            int citizenId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AssignableStaffDto>?> GetAssignableStaffAsync(
            int citizenId,
            CancellationToken cancellationToken = default);

        Task<CitizenStaffAssignmentDto?> AssignStaffAsync(
            int citizenId,
            AssignStaffToCitizenRequest request,
            CancellationToken cancellationToken = default);

        Task<CitizenStaffAssignmentDto?> UnassignStaffAsync(
            int citizenId,
            int citizenStaffAssignmentId,
            CancellationToken cancellationToken = default);
    }
}
