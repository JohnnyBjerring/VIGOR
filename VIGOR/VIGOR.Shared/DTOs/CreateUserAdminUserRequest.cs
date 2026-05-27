namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC13: Simpel oprettelse af bruger + medarbejdertilknytning.
    /// Serveren validerer rolle og afdeling.
    /// </summary>
    public class CreateUserAdminUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}
