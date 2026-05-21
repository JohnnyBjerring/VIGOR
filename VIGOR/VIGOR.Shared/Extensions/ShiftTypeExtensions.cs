using VIGOR.Shared.Enums;

namespace VIGOR.Shared.Extensions
{
    public static class ShiftTypeExtensions
    {
        public static string ToDisplayName(this ShiftType shiftType)
        {
            return shiftType switch
            {
                ShiftType.Day => "Dagvagt",
                ShiftType.Evening => "Aftenvagt",
                ShiftType.Night => "Nattevagt",
                _ => "Ukendt vagt"
            };
        }
    }
}
