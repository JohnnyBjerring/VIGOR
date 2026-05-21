using VIGOR.Shared.Enums;

namespace VIGOR.Shared.DTOs
{
    public class PnMedicationDto
    {
        public int PnMedicationId { get; set; }
        public int CitizenId { get; set; }
        public int DepartmentId { get; set; }

        public ShiftType ShiftType { get; set; }
        public string ShiftDisplayName { get; set; } = string.Empty;

        public string MedicineName { get; set; } = string.Empty;
        public string Dose { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;

        public DateTime GivenAtUtc { get; set; }
        public string GivenByUserId { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
