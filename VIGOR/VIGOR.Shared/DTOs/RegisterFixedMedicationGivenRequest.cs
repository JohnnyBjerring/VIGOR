namespace VIGOR.Shared.DTOs
{
    public class RegisterFixedMedicationGivenRequest
    {
        // Optional: hvis ikke angivet, bruger serveren "nu".
        public DateTime? GivenAt { get; set; }
    }
}
