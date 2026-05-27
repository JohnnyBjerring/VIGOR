namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC13: Afdeling til visning og oprettelse af medarbejdertilknytning.
    /// </summary>
    public class UserAdminDepartmentDto
    {
        public int DepartmentId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
