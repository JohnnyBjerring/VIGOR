using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Session-/runtime-state for den aktuelt valgte vagt i klienten.
    /// Senere overlap-, note- og opgaveflows kan afhænge af denne abstraktion.
    /// </summary>
    public interface ISelectedShiftState
    {
        SelectedShiftDto? CurrentShift { get; }
        bool HasSelectedShift { get; }
        void SetSelectedShift(SelectedShiftDto selectedShift);
        void Clear();
    }
}
