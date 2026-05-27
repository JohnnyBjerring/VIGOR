namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC12: Request til tildeling af arbejdstelefon til medarbejder.
    /// </summary>
    public class AssignWorkPhoneRequest
    {
        public int WorkPhoneId { get; set; }
        public int EmployeeId { get; set; }
    }
}
