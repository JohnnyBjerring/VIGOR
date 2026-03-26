using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.Interfaces.Services;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// Application service responsible for use-case logic around citizens.
    /// Iteration 2: Henter borgere for en given afdeling.
    /// Følger eksisterende projektstil: interface i Shared, implementering i Web.Services.
    /// </summary>
    public class CitizenService : ICitizenService
    {
        private readonly AppDbContext _context;

        public CitizenService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Henter borgere tilhørende en given afdeling (DepartmentId).
        /// Returnerer en liste af Citizen-entiteter; inkluderer ingen ekstra data.
        /// </summary>
        public async Task<IEnumerable<Citizen>> GetCitizensByDepartmentAsync(int departmentId, CancellationToken cancellationToken = default)
        {
            return await _context.Citizens
                .AsNoTracking()
                .Where(c => c.DepartmentId == departmentId)
                .ToListAsync(cancellationToken);
        }
    }
}
