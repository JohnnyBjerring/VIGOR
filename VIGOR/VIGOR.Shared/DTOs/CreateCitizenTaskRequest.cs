using VIGOR.Shared.Enums;

namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// UC10: Klientens request til at oprette en opgave på en borger.
    /// Bruger, afdeling og borgeradgang udledes server-side.
    /// </summary>
    public class CreateCitizenTaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ShiftType ShiftType { get; set; }
    }
}
