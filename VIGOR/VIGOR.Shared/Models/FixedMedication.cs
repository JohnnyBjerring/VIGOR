namespace VIGOR.Shared.Models
{
    /// <summary>
    /// Fast medicin for UC04.
    /// En række repræsenterer en medicinplan for en borger samt den seneste registrering.
    /// Historik/audit udvides senere i UC06, men UC04 gemmer stadig brugerreference og tidspunkt for seneste registrering.
    /// </summary>
    public class FixedMedication
    {
        public int FixedMedicationId { get; set; }

        public int CitizenId { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Planlagt tidspunkt. I UC04 bruges dato-delen ikke som aktiv dag, men feltet bevares for eksisterende datamodel.
        /// UI viser derfor kun klokkeslættet.
        /// </summary>
        public DateTime PlannedAt { get; set; }

        /// <summary>
        /// Enkel visningstekst for medicinplanen. I UC04 er dette ikke en frekvensmotor.
        /// Én række repræsenterer ét fast medicintidspunkt.
        /// </summary>
        public string ScheduleDescription { get; set; } = "Dagligt";

        public bool IsActive { get; set; } = true;

        public bool IsGiven { get; set; }
        public DateTime? GivenAt { get; set; }

        // Sporbarhed: Identity user-id fra JWT (NameIdentifier)
        public string? GivenByUserId { get; set; }
    }
}
