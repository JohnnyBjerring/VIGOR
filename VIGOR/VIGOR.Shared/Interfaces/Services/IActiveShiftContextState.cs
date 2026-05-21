using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Scoped runtime-state for brugerens aktive vagtkontekst.
    /// UC07 gemmer ikke en fuld vagtplan i databasen; state bruges til at gøre valget tilgængeligt for efterfølgende flows.
    /// </summary>
    public interface IActiveShiftContextState
    {
        ActiveShiftContextDto? Current { get; }
        bool HasActiveShift { get; }
        event Action? OnChange;

        Task SetAsync(ActiveShiftContextDto context);
        Task ClearAsync();
    }
}
