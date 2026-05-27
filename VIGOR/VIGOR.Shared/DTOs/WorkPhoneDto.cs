namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC12: Læsemodel for en arbejdstelefon og dens eventuelle aktive tildeling.
    /// </summary>
    public class WorkPhoneDto
    {
        public int WorkPhoneId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public bool IsAssigned { get; set; }
        public int? ActivePhoneAssignmentId { get; set; }
        public int? AssignedEmployeeId { get; set; }
        public string? AssignedEmployeeName { get; set; }
        public int? AssignedDepartmentId { get; set; }
        public DateTime? AssignedAtUtc { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(Label)
            ? PhoneNumber
            : $"{Label} ({PhoneNumber})";
    }
}
