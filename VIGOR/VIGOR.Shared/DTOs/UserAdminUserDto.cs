namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC13: Læsemodel til simpel brugeradministration.
    /// Viser Identity-bruger, roller og eventuel medarbejder-/afdelingstilknytning.
    /// </summary>
    public class UserAdminUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public int? EmployeeId { get; set; }
        public string? DisplayName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }

        public int? ActivePhoneAssignmentId { get; set; }
        public int? ActiveWorkPhoneId { get; set; }
        public string? ActivePhoneDisplayName { get; set; }

        public List<string> Roles { get; set; } = new();
        public string PrimaryRole => Roles.FirstOrDefault() ?? string.Empty;

        public bool IsActive { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
    }
}
