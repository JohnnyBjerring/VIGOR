using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Client-facing API for UC10 opgaver. Klienten sender titel/beskrivelse og aktiv vagttype;
    /// serveren udleder bruger, afdeling og adgang fra auth-context.
    /// </summary>
    public interface ICitizenTaskApi
    {
        Task<IReadOnlyList<CitizenTaskDto>?> GetTasksForCitizenAsync(
            int citizenId,
            CancellationToken cancellationToken = default);

        Task<CitizenTaskDto?> CreateTaskAsync(
            int citizenId,
            CreateCitizenTaskRequest request,
            CancellationToken cancellationToken = default);

        Task<CitizenTaskDto?> CompleteTaskAsync(
            int citizenId,
            int citizenTaskId,
            CancellationToken cancellationToken = default);
    }
}
