using VIGOR.Shared.DTOs;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// Server-intern UC10 service. Controller udleder afdeling og bruger-id fra auth-context.
    /// </summary>
    public interface ICitizenTaskService
    {
        Task<IReadOnlyList<CitizenTaskDto>?> GetForCitizenAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken = default);

        Task<CitizenTaskDto?> CreateAsync(
            int citizenId,
            int departmentId,
            string createdByUserId,
            CreateCitizenTaskRequest request,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null);

        Task<CitizenTaskDto?> CompleteAsync(
            int citizenId,
            int citizenTaskId,
            int departmentId,
            string completedByUserId,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null);
    }
}
