using VIGOR.Shared.Enums;

namespace VIGOR.Shared.DTOs
{
    /// <summary>
    /// Request DTO til UC07: brugeren vælger den aktive vagttype.
    /// Afdeling og bruger sendes ikke fra klienten; serveren udleder dem fra auth-context.
    /// </summary>
    public class SelectShiftRequest
    {
        public ShiftType ShiftType { get; set; }
    }
}
