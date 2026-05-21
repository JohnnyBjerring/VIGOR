namespace VIGOR.Shared.Enums
{
    public static class ShiftTypeExtensions
    {
        public static string ToDanishDisplayName(this ShiftType shiftType)
        {
            return shiftType switch
            {
                ShiftType.Day => "Dagvagt",
                ShiftType.Evening => "Aftenvagt",
                ShiftType.Night => "Nattevagt",
                _ => "Ukendt vagt"
            };
        }

        public static string ToShortDanishDisplayName(this ShiftType shiftType)
        {
            return shiftType switch
            {
                ShiftType.Day => "Dag",
                ShiftType.Evening => "Aften",
                ShiftType.Night => "Nat",
                _ => "Ukendt"
            };
        }

        /// <summary>
        /// UX-hjælp: foreslår vagttype ud fra lokalt klokkeslæt.
        /// Brugeren skal stadig bekræfte manuelt, fordi vagtskifte kan ske før/efter planlagt tidspunkt.
        /// </summary>
        public static ShiftType SuggestFromLocalTime(TimeSpan localTime)
        {
            if (localTime >= TimeSpan.FromHours(7) && localTime < TimeSpan.FromHours(15))
            {
                return ShiftType.Day;
            }

            if (localTime >= TimeSpan.FromHours(15) && localTime < TimeSpan.FromHours(23))
            {
                return ShiftType.Evening;
            }

            return ShiftType.Night;
        }
    }
}
