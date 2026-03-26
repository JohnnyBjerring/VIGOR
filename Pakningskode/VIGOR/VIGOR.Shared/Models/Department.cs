namespace VIGOR.Shared.Models
{
    /// <summary>
    /// Minimal Department-entitet for iteration 2 relation til Citizen.
    /// Indeholder kun det, der er nødvendigt for UC02: Id og navn.
    /// </summary>
    public class Department
    {
        public int DepartmentId { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Navigationsegenskab: én Department kan have 0..* Citizens.
        /// EF Core-venlig samling initialiseret til en tom liste.
        /// </summary>
        public List<Citizen> Citizens { get; set; } = new();
    }
}
