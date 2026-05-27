using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// Server-intern UC06 audit-service.
    /// Klienten må ikke oprette audit-events direkte. Audit oprettes af serverens runtime-paths.
    /// </summary>
    public interface IAuditService
    {
        Task<IReadOnlyList<AuditEventDto>?> GetForCitizenAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken = default);

        Task LogCitizenStatusUpdatedAsync(
            int citizenId,
            int departmentId,
            string userId,
            string? userDisplayNameSnapshot,
            CitizenStatus newStatus,
            ShiftType? shiftType = null,
            CancellationToken cancellationToken = default);

        Task LogFixedMedicationGivenAsync(
            int citizenId,
            int fixedMedicationId,
            string medicationName,
            int departmentId,
            string userId,
            string? userDisplayNameSnapshot,
            ShiftType? shiftType = null,
            CancellationToken cancellationToken = default);

        Task LogFixedMedicationCancelledAsync(
            int citizenId,
            int fixedMedicationId,
            string medicationName,
            int departmentId,
            string userId,
            string? userDisplayNameSnapshot,
            ShiftType? shiftType = null,
            CancellationToken cancellationToken = default);

        Task LogPnMedicationRegisteredAsync(
            int citizenId,
            int pnMedicationId,
            string medicationName,
            int departmentId,
            string userId,
            string? userDisplayNameSnapshot,
            ShiftType shiftType,
            CancellationToken cancellationToken = default);

        Task LogNoteCreatedAsync(
            int citizenId,
            int noteId,
            int departmentId,
            string userId,
            string? userDisplayNameSnapshot,
            ShiftType shiftType,
            CancellationToken cancellationToken = default);

        Task LogCitizenTaskCreatedAsync(
            int citizenId,
            int citizenTaskId,
            string title,
            int departmentId,
            string userId,
            string? userDisplayNameSnapshot,
            ShiftType shiftType,
            CancellationToken cancellationToken = default);

        Task LogCitizenTaskCompletedAsync(
            int citizenId,
            int citizenTaskId,
            string title,
            int departmentId,
            string userId,
            string? userDisplayNameSnapshot,
            ShiftType shiftType,
            CancellationToken cancellationToken = default);

        Task LogStaffAssignedToCitizenAsync(
            int citizenId,
            int citizenStaffAssignmentId,
            string employeeName,
            int departmentId,
            string userId,
            string? userDisplayNameSnapshot,
            CancellationToken cancellationToken = default);

        Task LogStaffUnassignedFromCitizenAsync(
            int citizenId,
            int citizenStaffAssignmentId,
            string employeeName,
            int departmentId,
            string userId,
            string? userDisplayNameSnapshot,
            CancellationToken cancellationToken = default);
    }
}
