namespace VIGOR.Shared.Models
{
    /// <summary>
    /// Domænemodel for en rolle i systemet.
    /// Level bruges til rolle-hierarki: Leder (3) > Vagtansvarlig (2) > Personale (1).
    /// </summary>
    public class Role
    {
        public int RoleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
    }
}
