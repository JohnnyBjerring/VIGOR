namespace VIGOR.Shared.Enums
{
    /// <summary>
    /// UC07: Fast sæt af vagttyper i overlapssystemet.
    /// Dansk visning håndteres via ShiftTypeExtensions, så kode og UI holdes adskilt.
    /// </summary>
    public enum ShiftType
    {
        Day = 1,
        Evening = 2,
        Night = 3
    }
}
