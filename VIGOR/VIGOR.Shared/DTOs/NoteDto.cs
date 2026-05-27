using VIGOR.Shared.Enums;

namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC09: DTO til visning af faglige noter på en borger.
    /// </summary>
    public class NoteDto
    {
        public int NoteId { get; set; }
        public int CitizenId { get; set; }
        public int DepartmentId { get; set; }

        public ShiftType ShiftType { get; set; }
        public string ShiftDisplayName { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
        public string CreatedByUserId { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
