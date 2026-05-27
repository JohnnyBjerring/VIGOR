using VIGOR.Shared.DTOs;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// Server-intern UC09 service. Controller udleder afdeling og bruger-id fra auth-context.
    /// </summary>
    public interface INoteService
    {
        Task<IReadOnlyList<NoteDto>?> GetForCitizenAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken = default);

        Task<NoteDto?> CreateAsync(
            int citizenId,
            int departmentId,
            string createdByUserId,
            CreateNoteRequest request,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null);
    }
}
