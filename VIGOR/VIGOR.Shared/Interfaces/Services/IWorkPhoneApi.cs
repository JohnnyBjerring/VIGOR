using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Client-side API-kontrakt for UC12 arbejdstelefoner.
    /// </summary>
    public interface IWorkPhoneApi
    {
        Task<IReadOnlyList<WorkPhoneDto>> GetPhonesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PhoneAssignmentDto>> GetActiveAssignmentsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PhoneAssignableEmployeeDto>> GetAssignableEmployeesAsync(CancellationToken cancellationToken = default);

        Task<WorkPhoneDto?> CreatePhoneAsync(
            CreateWorkPhoneRequest request,
            CancellationToken cancellationToken = default);

        Task<PhoneAssignmentDto?> AssignPhoneAsync(
            AssignWorkPhoneRequest request,
            CancellationToken cancellationToken = default);

        Task<PhoneAssignmentDto?> UnassignPhoneAsync(
            int phoneAssignmentId,
            CancellationToken cancellationToken = default);
    }
}
