using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Client-facing API til UC14 public/anonym oversigtsskærm.
    /// Endpointet er bevidst anonymt og må kun returnere anonymiserede DTO'er.
    /// </summary>
    public interface IPublicOverviewApi
    {
        Task<PublicOverviewDto?> GetPublicOverviewAsync(CancellationToken cancellationToken = default);
    }
}
