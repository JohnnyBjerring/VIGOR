namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC11: Medarbejdervalg til tildeling af personale til borger.
    /// Indeholder kun medarbejdere fra den afdeling, som serveren har udledt for den indloggede bruger.
    /// </summary>
    public class AssignableStaffDto
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? ActivePhoneDisplayName { get; set; }
    }
}
