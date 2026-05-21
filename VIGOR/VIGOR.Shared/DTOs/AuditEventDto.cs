using VIGOR.Shared.Enums;

namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC06: DTO til læsning af audit-events/historik.
    /// Audit oprettes server-side og må ikke oprettes direkte af klienten.
    /// </summary>
    public class AuditEventDto
    {
        public int AuditEventId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserDisplayNameSnapshot { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public int CitizenId { get; set; }
        public ShiftType? ShiftType { get; set; }
        public string? ShiftDisplayName { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
