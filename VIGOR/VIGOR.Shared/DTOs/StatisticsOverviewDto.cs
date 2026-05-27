namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC15: Simpel statistik for centrale runtime-hændelser.
    /// DTO'en indeholder kun aggregerede tal og ingen borgernavne, medicinnavne, noter eller brugerreferencer.
    /// </summary>
    public class StatisticsOverviewDto
    {
        public DateTime GeneratedAtUtc { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }

        public string ScopeDisplayName { get; set; } = string.Empty;
        public string AccessLevelDisplayName { get; set; } = string.Empty;
        public bool IsSystemWide { get; set; }
        public string DataProtectionNote { get; set; } = string.Empty;

        public int StatusChangeCount { get; set; }
        public int FixedMedicationRegistrationCount { get; set; }
        public int PnMedicationRegistrationCount { get; set; }
        public int TaskCreatedCount { get; set; }
        public int TaskCompletedCount { get; set; }
        public int OpenTaskCount { get; set; }
    }
}
