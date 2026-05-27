namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC12: Request til oprettelse af arbejdstelefon.
    /// </summary>
    public class CreateWorkPhoneRequest
    {
        public string Label { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
