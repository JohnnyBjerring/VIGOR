namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC11: DTO til visning af personale tildelt en borger.
    /// </summary>
    public class CitizenStaffAssignmentDto
    {
        public int CitizenStaffAssignmentId { get; set; }
        public int CitizenId { get; set; }
        public int DepartmentId { get; set; }

        public int EmployeeId { get; set; }
        public string EmployeeNameSnapshot { get; set; } = string.Empty;

        public string? ActivePhoneLabel { get; set; }
        public string? ActivePhoneNumber { get; set; }
        public string? ActivePhoneDisplayName { get; set; }

        public string AssignedByUserId { get; set; } = string.Empty;
        public DateTime AssignedAtUtc { get; set; }

        public bool IsActive { get; set; }
        public DateTime? UnassignedAtUtc { get; set; }
        public string? UnassignedByUserId { get; set; }
    }
}
