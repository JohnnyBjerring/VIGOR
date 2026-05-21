namespace VIGOR.Shared.DTOs
{
    public class UpdateFixedMedicationPlanRequest
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Kun klokkeslættet bruges i UC04. Dato-delen normaliseres på serveren.
        /// </summary>
        public DateTime PlannedAt { get; set; }

        public string ScheduleDescription { get; set; } = "Dagligt";

        public bool IsActive { get; set; } = true;
    }
}
