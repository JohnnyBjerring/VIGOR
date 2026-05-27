using VIGOR.Shared.Enums;

namespace VIGOR.Shared.Models
{
    /// <summary>
    /// UC09: Faglig note knyttet til en borger.
    /// En note oprettes af personale under en aktiv vagt og gemmes med bruger-, afdeling- og tidskontekst.
    /// </summary>
    public class Note
    {
        public int NoteId { get; set; }

        public int CitizenId { get; set; }
        public int DepartmentId { get; set; }

        public ShiftType ShiftType { get; set; }

        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Identity user-id fra den bruger, der oprettede noten.
        /// Klienten sender ikke dette felt; serveren udleder det fra JWT/auth-context.
        /// </summary>
        public string CreatedByUserId { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }
}
