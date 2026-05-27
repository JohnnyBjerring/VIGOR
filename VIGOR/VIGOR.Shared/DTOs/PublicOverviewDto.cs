namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC14: Anonym public oversigt.
    /// Indeholder kun aggregerede statusdata og anonymiserede statusrækker.
    /// </summary>
    public class PublicOverviewDto
    {
        public DateTime GeneratedAtUtc { get; set; }
        public int TotalCitizenCount { get; set; }
        public int GreenCount { get; set; }
        public int YellowCount { get; set; }
        public int RedCount { get; set; }
        public IReadOnlyList<PublicCitizenStatusDto> Citizens { get; set; } = new List<PublicCitizenStatusDto>();
    }
}
