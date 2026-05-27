using VIGOR.Shared.Enums;

namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC14: Anonym statusrække til public/kiosk view.
    /// DTO'en må ikke indeholde CitizenId, navn, afdeling, medicin, noter, opgaver eller brugerreference.
    /// </summary>
    public class PublicCitizenStatusDto
    {
        public int SortOrder { get; set; }
        public string DisplayLabel { get; set; } = string.Empty;
        public CitizenStatus Status { get; set; }
        public string StatusDisplayName { get; set; } = string.Empty;
        public string AttentionLevel { get; set; } = string.Empty;
    }
}
