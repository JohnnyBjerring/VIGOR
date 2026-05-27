using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Web.Data;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC14: Bygger et anonymt public/kiosk overblik.
    /// Returnerer ikke CitizenId, borgernavn, afdeling, medicin, noter, opgaver, auditbeskrivelser eller brugerreference.
    /// </summary>
    public class PublicOverviewService : IPublicOverviewService
    {
        private readonly AppDbContext _context;

        public PublicOverviewService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PublicOverviewDto> GetPublicOverviewAsync(CancellationToken cancellationToken = default)
        {
            var citizens = await _context.Citizens
                .AsNoTracking()
                .Select(c => new
                {
                    c.CitizenId,
                    c.Status
                })
                .ToListAsync(cancellationToken);

            var rows = citizens
                .OrderBy(c => GetStatusSortKey(c.Status))
                .ThenBy(c => c.CitizenId)
                .Select((citizen, index) => new PublicCitizenStatusDto
                {
                    SortOrder = index + 1,
                    DisplayLabel = $"Borger {index + 1}",
                    Status = citizen.Status,
                    StatusDisplayName = ToDanishStatus(citizen.Status),
                    AttentionLevel = ToAttentionLevel(citizen.Status)
                })
                .ToList();

            return new PublicOverviewDto
            {
                GeneratedAtUtc = DateTime.UtcNow,
                TotalCitizenCount = rows.Count,
                GreenCount = rows.Count(c => c.Status == CitizenStatus.Green),
                YellowCount = rows.Count(c => c.Status == CitizenStatus.Yellow),
                RedCount = rows.Count(c => c.Status == CitizenStatus.Red),
                Citizens = rows
            };
        }

        private static int GetStatusSortKey(CitizenStatus status)
        {
            return status switch
            {
                CitizenStatus.Red => 0,
                CitizenStatus.Yellow => 1,
                CitizenStatus.Green => 2,
                _ => 3
            };
        }

        private static string ToDanishStatus(CitizenStatus status)
        {
            return status switch
            {
                CitizenStatus.Green => "Grøn",
                CitizenStatus.Yellow => "Gul",
                CitizenStatus.Red => "Rød",
                _ => status.ToString()
            };
        }

        private static string ToAttentionLevel(CitizenStatus status)
        {
            return status switch
            {
                CitizenStatus.Red => "Kræver særlig opmærksomhed",
                CitizenStatus.Yellow => "Kræver opmærksomhed",
                CitizenStatus.Green => "Normal drift",
                _ => "Ukendt"
            };
        }
    }
}
