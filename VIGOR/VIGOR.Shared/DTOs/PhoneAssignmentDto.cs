namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC12: Læsemodel for telefon-tildeling til en medarbejder.
    /// </summary>
    public class PhoneAssignmentDto
    {
        public int PhoneAssignmentId { get; set; }
        public int WorkPhoneId { get; set; }
        public string PhoneLabelSnapshot { get; set; } = string.Empty;
        public string PhoneNumberSnapshot { get; set; } = string.Empty;

        public int EmployeeId { get; set; }
        public string EmployeeNameSnapshot { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }

        public string AssignedByUserId { get; set; } = string.Empty;
        public DateTime AssignedAtUtc { get; set; }
        public bool IsActive { get; set; }
        public DateTime? UnassignedAtUtc { get; set; }
        public string? UnassignedByUserId { get; set; }

        public string PhoneDisplayName => string.IsNullOrWhiteSpace(PhoneLabelSnapshot)
            ? PhoneNumberSnapshot
            : $"{PhoneLabelSnapshot} ({PhoneNumberSnapshot})";
    }
}
