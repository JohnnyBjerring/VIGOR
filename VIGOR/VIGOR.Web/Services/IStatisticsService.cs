using VIGOR.Shared.DTOs;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC15 server-side service til statistik og rapporter.
    /// </summary>
    public interface IStatisticsService
    {
        Task<StatisticsOverviewDto> GetDepartmentStatisticsAsync(
            int departmentId,
            string departmentName,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            CancellationToken cancellationToken = default);

        Task<StatisticsOverviewDto> GetSystemStatisticsAsync(
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            CancellationToken cancellationToken = default);
    }
}
