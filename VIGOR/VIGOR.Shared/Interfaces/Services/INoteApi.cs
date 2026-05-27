using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Client-facing API for UC09 noter. Klienten sender noteindhold og aktiv vagttype;
    /// serveren udleder bruger, afdeling og adgang fra auth-context.
    /// </summary>
    public interface INoteApi
    {
        Task<IReadOnlyList<NoteDto>?> GetNotesForCitizenAsync(
            int citizenId,
            CancellationToken cancellationToken = default);

        Task<NoteDto?> CreateNoteAsync(
            int citizenId,
            CreateNoteRequest request,
            CancellationToken cancellationToken = default);
    }
}
