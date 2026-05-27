using VIGOR.Shared.DTOs;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// Server-intern UC12 service for arbejdstelefoner og telefontildelinger.
    /// </summary>
    public interface IWorkPhoneService
    {
        Task<IReadOnlyList<WorkPhoneDto>> GetPhonesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PhoneAssignmentDto>> GetActiveAssignmentsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PhoneAssignableEmployeeDto>> GetAssignableEmployeesAsync(CancellationToken cancellationToken = default);

        Task<WorkPhoneDto> CreatePhoneAsync(
            CreateWorkPhoneRequest request,
            CancellationToken cancellationToken = default);

        Task<PhoneAssignmentDto?> AssignPhoneAsync(
            AssignWorkPhoneRequest request,
            string assignedByUserId,
            CancellationToken cancellationToken = default);

        Task<PhoneAssignmentDto?> UnassignPhoneAsync(
            int phoneAssignmentId,
            string unassignedByUserId,
            CancellationToken cancellationToken = default);
    }
}
