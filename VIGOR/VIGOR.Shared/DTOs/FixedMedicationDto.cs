namespace VIGOR.Shared.DTOs
{
    public class FixedMedicationDto
    {
        public int FixedMedicationId { get; set; }
        public int CitizenId { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime PlannedAt { get; set; }

        public string ScheduleDescription { get; set; } = "Dagligt";
        public bool IsActive { get; set; } = true;

        public bool IsGiven { get; set; }
        public DateTime? GivenAt { get; set; }

        public string? GivenByUserId { get; set; }
    }
}
