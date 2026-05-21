using VIGOR.Shared.Enums;

namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// Aktiv arbejdskontekst for UC07.
    /// Den er ikke en fuld databasebaseret vagtplan endnu, men en runtime-kontekst som senere flows kan kobles til.
    /// </summary>
    public class ActiveShiftContextDto
    {
        public ShiftType ShiftType { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public DateTime SelectedAtUtc { get; set; }
        public string SelectedByUserId { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
    }
}
