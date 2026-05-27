using VIGOR.Shared.DTOs;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// Server-intern UC11 service. Controller udleder afdeling og bruger-id fra auth-context.
    /// </summary>
    public interface IStaffAssignmentService
    {
        Task<IReadOnlyList<CitizenStaffAssignmentDto>?> GetForCitizenAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AssignableStaffDto>?> GetAssignableStaffAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken = default);

        Task<CitizenStaffAssignmentDto?> AssignAsync(
            int citizenId,
            int departmentId,
            int employeeId,
            string assignedByUserId,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null);

        Task<CitizenStaffAssignmentDto?> UnassignAsync(
            int citizenId,
            int citizenStaffAssignmentId,
            int departmentId,
            string unassignedByUserId,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null);
    }
}
