namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC12: Medarbejdervalg til telefontildeling.
    /// </summary>
    public class PhoneAssignableEmployeeDto
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? ActivePhoneDisplayName { get; set; }
    }
}
