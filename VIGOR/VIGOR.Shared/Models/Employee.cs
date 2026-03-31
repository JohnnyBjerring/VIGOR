namespace VIGOR.Shared.Models
{
    /// <summary>
    /// Medarbejder-entitet – har 1:1 relation til IdentityUser.
    /// </summary>
    public class Employee
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; } = string.Empty;

        // FK til IdentityUser
        public string IdentityUserId { get; set; } = string.Empty;

        // FK til Department
        public int? DepartmentId { get; set; }
    }
}
