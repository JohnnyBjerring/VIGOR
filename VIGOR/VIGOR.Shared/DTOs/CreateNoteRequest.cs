using VIGOR.Shared.Enums;

namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC09: Klientens request til at oprette en note.
    /// Bruger, afdeling og borgeradgang udledes server-side.
    /// </summary>
    public class CreateNoteRequest
    {
        public string Content { get; set; } = string.Empty;
        public ShiftType ShiftType { get; set; }
    }
}
