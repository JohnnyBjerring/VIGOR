namespace VIGOR.Shared.Models
{
    /// <summary>
    /// UC12: Arbejdstelefon, som kan tildeles en medarbejder i en vagt-/driftskontekst.
    /// Telefonnummeret er ikke knyttet til borgere direkte, men kan vises sammen med tildelt personale.
    /// </summary>
    public class WorkPhone
    {
        public int WorkPhoneId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
