namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC11: Request til at tildele en medarbejder til en borger.
    /// DepartmentId og brugerreference sendes ikke fra klienten, men udledes server-side.
    /// </summary>
    public class AssignStaffToCitizenRequest
    {
        public int EmployeeId { get; set; }
    }
}
