using VIGOR.Shared.Enums;

namespace VIGOR.Shared.Models
{
    /// <summary>
    /// UC10: Opgave knyttet til en borger.
    /// En opgave oprettes af personale under en aktiv vagt og kan markeres som afsluttet senere.
    /// </summary>
    public class CitizenTask
    {
        public int CitizenTaskId { get; set; }

        public int CitizenId { get; set; }
        public int DepartmentId { get; set; }

        public ShiftType ShiftType { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Identity user-id fra den bruger, der oprettede opgaven.
        /// Klienten sender ikke dette felt; serveren udleder det fra JWT/auth-context.
        /// </summary>
        public string CreatedByUserId { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public bool IsCompleted { get; set; }
        public DateTime? CompletedAtUtc { get; set; }

        /// <summary>
        /// Identity user-id fra den bruger, der afsluttede opgaven.
        /// Klienten sender ikke dette felt; serveren udleder det fra JWT/auth-context.
        /// </summary>
        public string? CompletedByUserId { get; set; }
    }
}
