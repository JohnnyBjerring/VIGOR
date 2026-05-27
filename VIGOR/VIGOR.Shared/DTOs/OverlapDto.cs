using VIGOR.Shared.Enums;

namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC08: Samlet overlapvisning for den indloggede brugers afdeling.
    /// DTO'en er read-only set fra klienten og samler data, der allerede findes i systemet.
    /// </summary>
    public class OverlapDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;

        public ShiftType? ActiveShiftType { get; set; }
        public string? ActiveShiftDisplayName { get; set; }

        public DateTime GeneratedAtUtc { get; set; }

        public IReadOnlyList<CitizenOverlapDto> Citizens { get; set; } = new List<CitizenOverlapDto>();
    }
}
