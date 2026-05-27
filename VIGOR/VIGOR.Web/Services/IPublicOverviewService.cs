using VIGOR.Shared.DTOs;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC14 server-side service til anonym public oversigt.
    /// Servicen returnerer bevidst kun anonyme DTO'er uden personhenførbare data.
    /// </summary>
    public interface IPublicOverviewService
    {
        Task<PublicOverviewDto> GetPublicOverviewAsync(CancellationToken cancellationToken = default);
    }
}
