namespace VIGOR.Shared.Models
{
    /// <summary>
    /// Borger-entitet til brug i UC02 – Se borgere på afdeling.
    /// Indeholder kun de properties, der er nødvendige i iteration 2.
    /// </summary>
    public class Citizen
    {
        /// <summary>
        /// Unik identifikation af borgeren.
        /// Holder sig til int-navngivningsmønsteret brugt i andre entiteter.
        /// </summary>
        public int CitizenId { get; set; }

        /// <summary>
        /// Borgerens fulde navn.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Borgerens status (f.eks. Aktiv, Inaktiv).
        /// </summary>
        public string Status { get; set; } = "Aktiv";

        /// <summary>
        /// FK til den afdeling (Department) borgeren tilhører.
        /// Hver borger skal tilhøre præcis én afdeling i iteration 2.
        /// </summary>
        public int DepartmentId { get; set; }

        /// <summary>
        /// Navigationsegenskab til Department.
        /// EF Core-venlig non-null reference, da relationen er obligatorisk.
        /// </summary>
        public Department Department { get; set; } = null!;
    }
}
