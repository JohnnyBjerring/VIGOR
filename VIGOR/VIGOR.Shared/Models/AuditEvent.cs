using VIGOR.Shared.Enums;

namespace VIGOR.Shared.Models
{
    /// <summary>
    /// UC06: Append-only audit-event for centrale handlinger i VIGOR.
    /// En række beskriver hvem der gjorde hvad, hvornår, på hvilken borger og i hvilken kontekst.
    /// </summary>
    public class AuditEvent
    {
        public int AuditEventId { get; set; }

        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Identity user-id fra den autentificerede bruger. Klienten må ikke sende dette felt.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Læsbart øjebliksbillede af brugeren/medarbejderen på registreringstidspunktet.
        /// Gør historikken forståelig, selv hvis brugeren senere ændrer navn eller deaktiveres.
        /// </summary>
        public string UserDisplayNameSnapshot { get; set; } = string.Empty;

        public int DepartmentId { get; set; }
        public int CitizenId { get; set; }
        public ShiftType? ShiftType { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
