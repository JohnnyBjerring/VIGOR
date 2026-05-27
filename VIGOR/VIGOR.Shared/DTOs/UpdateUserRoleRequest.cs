namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC13: Request til at sætte brugerens primære rolle.
    /// I projektet holdes scope til én aktiv rolle pr. bruger.
    /// </summary>
    public class UpdateUserRoleRequest
    {
        public string RoleName { get; set; } = string.Empty;
    }
}
