using VIGOR.Shared.DTOs;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.Shared.Services
{
    /// <summary>
    /// Holder aktiv vagtkontekst i scoped runtime-state.
    /// Single source of truth i klienten for den valgte vagt i den aktuelle session.
    /// </summary>
    public class ActiveShiftContextState : IActiveShiftContextState
    {
        private ActiveShiftContextDto? _current;

        public ActiveShiftContextDto? Current => _current;
        public bool HasActiveShift => _current != null;

        public event Action? OnChange;

        public Task SetAsync(ActiveShiftContextDto context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _current = context;
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _current = null;
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}
