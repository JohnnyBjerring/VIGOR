using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.Constants;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC15: Simpel statistik baseret på eksisterende audit-events og opgaver.
    /// Servicen returnerer kun aggregerede tal og ingen borgernavne, medicinnavne, noteindhold eller brugerreferencer.
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        private readonly AppDbContext _context;

        public StatisticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<StatisticsOverviewDto> GetDepartmentStatisticsAsync(
            int departmentId,
            string departmentName,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            CancellationToken cancellationToken = default)
        {
            var auditEvents = ApplyDateRange(_context.AuditEvents.AsNoTracking(), fromUtc, toUtc)
                .Where(e => e.DepartmentId == departmentId);

            var openTasks = ApplyDateRange(_context.CitizenTasks.AsNoTracking(), fromUtc, toUtc)
                .Where(t => t.DepartmentId == departmentId && !t.IsCompleted);

            return await BuildStatisticsAsync(
                auditEvents,
                openTasks,
                scopeDisplayName: departmentName,
                accessLevelDisplayName: "Leder – afdelingsstatistik",
                isSystemWide: false,
                dataProtectionNote: "Statistikken vises som aggregerede tal for lederens afdeling og indeholder ikke borgernavne, medicinnavne, noter eller brugerreferencer.",
                fromUtc,
                toUtc,
                cancellationToken);
        }

        public async Task<StatisticsOverviewDto> GetSystemStatisticsAsync(
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            CancellationToken cancellationToken = default)
        {
            var auditEvents = ApplyDateRange(_context.AuditEvents.AsNoTracking(), fromUtc, toUtc);
            var openTasks = ApplyDateRange(_context.CitizenTasks.AsNoTracking(), fromUtc, toUtc)
                .Where(t => !t.IsCompleted);

            return await BuildStatisticsAsync(
                auditEvents,
                openTasks,
                scopeDisplayName: "Hele systemet",
                accessLevelDisplayName: "Superbruger – anonym systemstatistik",
                isSystemWide: true,
                dataProtectionNote: "Superbruger ser kun anonyme systemtal. Statistikken indeholder ikke borgernavne, afdelingsnavne, medicinnavne, noter, opgavetekster eller brugerreferencer.",
                fromUtc,
                toUtc,
                cancellationToken);
        }

        private static IQueryable<AuditEvent> ApplyDateRange(
            IQueryable<AuditEvent> query,
            DateTime? fromUtc,
            DateTime? toUtc)
        {
            if (fromUtc.HasValue)
            {
                query = query.Where(e => e.CreatedAtUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(e => e.CreatedAtUtc < toUtc.Value);
            }

            return query;
        }

        private static IQueryable<CitizenTask> ApplyDateRange(
            IQueryable<CitizenTask> query,
            DateTime? fromUtc,
            DateTime? toUtc)
        {
            if (fromUtc.HasValue)
            {
                query = query.Where(t => t.CreatedAtUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(t => t.CreatedAtUtc < toUtc.Value);
            }

            return query;
        }

        private static async Task<StatisticsOverviewDto> BuildStatisticsAsync(
            IQueryable<AuditEvent> auditEvents,
            IQueryable<CitizenTask> openTasks,
            string scopeDisplayName,
            string accessLevelDisplayName,
            bool isSystemWide,
            string dataProtectionNote,
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken)
        {
            return new StatisticsOverviewDto
            {
                GeneratedAtUtc = DateTime.UtcNow,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                ScopeDisplayName = scopeDisplayName,
                AccessLevelDisplayName = accessLevelDisplayName,
                IsSystemWide = isSystemWide,
                DataProtectionNote = dataProtectionNote,
                StatusChangeCount = await auditEvents.CountAsync(e => e.Action == AuditActions.CitizenStatusUpdated, cancellationToken),
                FixedMedicationRegistrationCount = await auditEvents.CountAsync(e => e.Action == AuditActions.FixedMedicationGiven, cancellationToken),
                PnMedicationRegistrationCount = await auditEvents.CountAsync(e => e.Action == AuditActions.PnMedicationRegistered, cancellationToken),
                TaskCreatedCount = await auditEvents.CountAsync(e => e.Action == AuditActions.TaskCreated, cancellationToken),
                TaskCompletedCount = await auditEvents.CountAsync(e => e.Action == AuditActions.TaskCompleted, cancellationToken),
                OpenTaskCount = await openTasks.CountAsync(cancellationToken)
            };
        }
    }
}
