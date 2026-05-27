using VIGOR.Shared.Enums;

namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC08: Overlapdata for én borger.
    /// Indeholder kun data, som bruges i den interne, login-beskyttede overlapvisning.
    /// </summary>
    public class CitizenOverlapDto
    {
        public int CitizenId { get; set; }
        public string Name { get; set; } = string.Empty;

        public CitizenStatus Status { get; set; }
        public string StatusDisplayName { get; set; } = string.Empty;

        public IReadOnlyList<FixedMedicationDto> FixedMedications { get; set; } = new List<FixedMedicationDto>();
        public IReadOnlyList<PnMedicationDto> RecentPnMedications { get; set; } = new List<PnMedicationDto>();
        public IReadOnlyList<AuditEventDto> RecentAuditEvents { get; set; } = new List<AuditEventDto>();
        public IReadOnlyList<NoteDto> ActiveNotes { get; set; } = new List<NoteDto>();
        public IReadOnlyList<CitizenTaskDto> OpenTasks { get; set; } = new List<CitizenTaskDto>();
        public IReadOnlyList<CitizenStaffAssignmentDto> ActiveStaffAssignments { get; set; } = new List<CitizenStaffAssignmentDto>();

        public int FixedMedicationCount { get; set; }
        public int RecentPnMedicationCount { get; set; }
        public int RecentAuditEventCount { get; set; }
        public int ActiveNoteCount { get; set; }
        public int OpenTaskCount { get; set; }
        public int ActiveStaffAssignmentCount { get; set; }
    }
}
