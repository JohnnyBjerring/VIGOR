using VIGOR.Shared.DTOs;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.Shared.Services
{
    /// <summary>
    /// Simpel scoped klient-state for UC07. Valget lever i den aktuelle bruger-session/runtime.
    /// </summary>
    public class SelectedShiftState : ISelectedShiftState
    {
        public SelectedShiftDto? CurrentShift { get; private set; }

        public bool HasSelectedShift => CurrentShift != null;

        public void SetSelectedShift(SelectedShiftDto selectedShift)
        {
            CurrentShift = selectedShift ?? throw new ArgumentNullException(nameof(selectedShift));
        }

        public void Clear()
        {
            CurrentShift = null;
        }
    }
}
