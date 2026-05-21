using VIGOR.Shared.Enums;

namespace VIGOR.Shared.DTOs
{
    public class RegisterPnMedicationRequest
    {
        public string MedicineName { get; set; } = string.Empty;
        public string Dose { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;

        // Optional: hvis ikke angivet, bruger serveren nuværende UTC-tid.
        public DateTime? GivenAt { get; set; }

        public ShiftType ShiftType { get; set; }
    }
}
