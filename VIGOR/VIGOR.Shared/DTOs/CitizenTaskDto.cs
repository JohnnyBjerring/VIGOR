using VIGOR.Shared.Enums;

namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC10: DTO til visning af opgaver på en borger.
    /// </summary>
    public class CitizenTaskDto
    {
        public int CitizenTaskId { get; set; }
        public int CitizenId { get; set; }
        public int DepartmentId { get; set; }

        public ShiftType ShiftType { get; set; }
        public string ShiftDisplayName { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string CreatedByUserId { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }

        public bool IsCompleted { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? CompletedByUserId { get; set; }
    }
}
