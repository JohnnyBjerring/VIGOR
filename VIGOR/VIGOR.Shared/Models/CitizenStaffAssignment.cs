namespace VIGOR.Shared.Models
{
    /// <summary>
    /// UC11: Aktiv/personhistorisk tildeling af personale til en borger.
    /// Tildelingen gemmer både den tildelte medarbejder og hvem der oprettede/fjernede tildelingen.
    /// </summary>
    public class CitizenStaffAssignment
    {
        public int CitizenStaffAssignmentId { get; set; }

        public int CitizenId { get; set; }
        public int DepartmentId { get; set; }

        public int EmployeeId { get; set; }
        public string EmployeeNameSnapshot { get; set; } = string.Empty;

        /// <summary>
        /// Identity user-id fra den bruger, der oprettede tildelingen.
        /// Klienten sender ikke dette felt; serveren udleder det fra JWT/auth-context.
        /// </summary>
        public string AssignedByUserId { get; set; } = string.Empty;

        public DateTime AssignedAtUtc { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? UnassignedAtUtc { get; set; }

        /// <summary>
        /// Identity user-id fra den bruger, der fjernede tildelingen.
        /// Klienten sender ikke dette felt; serveren udleder det fra JWT/auth-context.
        /// </summary>
        public string? UnassignedByUserId { get; set; }
    }
}
