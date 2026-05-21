using VIGOR.Shared.Enums;

namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC07 response/state: den aktuelle valgte vagt for brugerens session.
    /// </summary>
    public class SelectedShiftDto
    {
        public ShiftType ShiftType { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public DateTime SelectedAtUtc { get; set; }
        public string SelectedByUserId { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
    }
}
