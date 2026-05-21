namespace VIGOR.Shared.Constants
{
    /// <summary>
    /// Faste action-navne til UC06 audit-log.
    /// De holdes som konstanter, så controller/service/tests ikke bruger løse tekststrenge flere steder.
    /// </summary>
    public static class AuditActions
    {
        public const string CitizenStatusUpdated = nameof(CitizenStatusUpdated);
        public const string FixedMedicationGiven = nameof(FixedMedicationGiven);
        public const string FixedMedicationCancelled = nameof(FixedMedicationCancelled);
        public const string PnMedicationRegistered = nameof(PnMedicationRegistered);
    }
}
