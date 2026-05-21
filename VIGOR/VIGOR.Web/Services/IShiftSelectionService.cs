using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;

namespace VIGOR.Web.Services
{
    public interface IShiftSelectionService
    {
        Task<ActiveShiftContextDto> SelectShiftAsync(
            ShiftType shiftType,
            string selectedByUserId,
            int departmentId,
            CancellationToken cancellationToken = default);
    }
}
