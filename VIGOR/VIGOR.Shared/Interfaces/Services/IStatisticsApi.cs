using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Client-side API-kontrakt for UC15 statistik og rapporter.
    /// </summary>
    public interface IStatisticsApi
    {
        Task<StatisticsOverviewDto?> GetStatisticsAsync(
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            CancellationToken cancellationToken = default);
    }
}
