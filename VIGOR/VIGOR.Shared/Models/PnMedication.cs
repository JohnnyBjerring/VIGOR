using VIGOR.Shared.Enums;

namespace VIGOR.Shared.Models
{
    /// <summary>
    /// UC05: Registrering af PN-medicin givet ved behov.
    /// En række repræsenterer én konkret PN-medicinregistrering, ikke en fuld medicinplan eller medicinkatalog.
    /// Audit/historik udbygges i UC06.
    /// </summary>
    public class PnMedication
    {
        public int PnMedicationId { get; set; }

        public int CitizenId { get; set; }
        public int DepartmentId { get; set; }

        public ShiftType ShiftType { get; set; }

        public string MedicineName { get; set; } = string.Empty;
        public string Dose { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;

        public DateTime GivenAtUtc { get; set; }
        public string GivenByUserId { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
