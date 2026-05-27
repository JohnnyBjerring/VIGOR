namespace VIGOR.Shared.Models
{
    /// <summary>
    /// UC12: Aktiv eller historisk tildeling af en arbejdstelefon til en medarbejder.
    /// Assignmenten er append-/historikorienteret: fjernelse markerer rækken inaktiv i stedet for at slette den.
    /// </summary>
    public class PhoneAssignment
    {
        public int PhoneAssignmentId { get; set; }
        public int WorkPhoneId { get; set; }
        public int EmployeeId { get; set; }
        public int? DepartmentId { get; set; }

        public string EmployeeNameSnapshot { get; set; } = string.Empty;
        public string PhoneLabelSnapshot { get; set; } = string.Empty;
        public string PhoneNumberSnapshot { get; set; } = string.Empty;

        public string AssignedByUserId { get; set; } = string.Empty;
        public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
        public DateTime? UnassignedAtUtc { get; set; }
        public string? UnassignedByUserId { get; set; }
    }
}
