using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.Enums;
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
        private readonly IAuditService? _auditService;

        public CitizenService(AppDbContext context, IAuditService? auditService = null)
        {
            _context = context;
            _auditService = auditService;
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

        public async Task<Citizen?> UpdateCitizenStatusAsync(int citizenId, int departmentId, CitizenStatus status, CancellationToken cancellationToken = default)
        {
            return await UpdateCitizenStatusInternalAsync(
                citizenId,
                departmentId,
                status,
                updatedByUserId: null,
                userDisplayNameSnapshot: null,
                shiftType: null,
                cancellationToken: cancellationToken);
        }

        public async Task<Citizen?> UpdateCitizenStatusAsync(
            int citizenId,
            int departmentId,
            CitizenStatus status,
            string updatedByUserId,
            string? userDisplayNameSnapshot = null,
            ShiftType? shiftType = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(updatedByUserId))
            {
                throw new ArgumentException("UpdatedByUserId er påkrævet ved audit-logget statusændring.", nameof(updatedByUserId));
            }

            return await UpdateCitizenStatusInternalAsync(
                citizenId,
                departmentId,
                status,
                updatedByUserId,
                userDisplayNameSnapshot,
                shiftType,
                cancellationToken);
        }

        private async Task<Citizen?> UpdateCitizenStatusInternalAsync(
            int citizenId,
            int departmentId,
            CitizenStatus status,
            string? updatedByUserId,
            string? userDisplayNameSnapshot,
            ShiftType? shiftType,
            CancellationToken cancellationToken)
        {
            var citizen = await _context.Citizens
                .FirstOrDefaultAsync(c => c.CitizenId == citizenId && c.DepartmentId == departmentId, cancellationToken);

            if (citizen == null)
            {
                return null;
            }

            citizen.Status = status;

            if (_auditService != null && !string.IsNullOrWhiteSpace(updatedByUserId))
            {
                await _auditService.LogCitizenStatusUpdatedAsync(
                    citizenId,
                    departmentId,
                    updatedByUserId,
                    userDisplayNameSnapshot,
                    status,
                    shiftType,
                    cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return citizen;
        }
    }
}
