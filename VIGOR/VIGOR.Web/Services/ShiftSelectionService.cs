using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC07 application service.
    /// Ansvar: validere vagtvalg og oprette aktiv vagtkontekst uden at blande UI eller controllerlogik ind.
    /// </summary>
    public class ShiftSelectionService : IShiftSelectionService
    {
        public Task<ActiveShiftContextDto> SelectShiftAsync(
            ShiftType shiftType,
            string selectedByUserId,
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined(typeof(ShiftType), shiftType))
            {
                throw new ArgumentException("Den valgte vagttype er ugyldig.", nameof(shiftType));
            }

            if (string.IsNullOrWhiteSpace(selectedByUserId))
            {
                throw new ArgumentException("Bruger kunne ikke fastlægges for vagtvalget.", nameof(selectedByUserId));
            }

            if (departmentId <= 0)
            {
                throw new ArgumentException("Afdeling kunne ikke fastlægges for vagtvalget.", nameof(departmentId));
            }

            var context = new ActiveShiftContextDto
            {
                ShiftType = shiftType,
                DisplayName = shiftType.ToDanishDisplayName(),
                SelectedAtUtc = DateTime.UtcNow,
                SelectedByUserId = selectedByUserId,
                DepartmentId = departmentId
            };

            return Task.FromResult(context);
        }
    }
}
