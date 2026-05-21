using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC06 audit-service.
    /// Log-metoderne tilføjer audit-events til den aktuelle DbContext. Den kaldende service gemmer med SaveChangesAsync,
    /// så domæneændringen og audit-eventet gemmes som én samlet runtime-handling.
    /// </summary>
    public class AuditService : IAuditService
    {
        private const int MaxEntityTypeLength = 80;
        private const int MaxActionLength = 80;
        private const int MaxDescriptionLength = 500;
        private const int MaxUserDisplayNameLength = 200;

        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<AuditEventDto>?> GetForCitizenAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            var citizenExists = await _context.Citizens
                .AsNoTracking()
                .AnyAsync(c => c.CitizenId == citizenId && c.DepartmentId == departmentId, cancellationToken);

            if (!citizenExists)
            {
                return null;
            }

            var events = await _context.AuditEvents
                .AsNoTracking()
                .Where(e => e.CitizenId == citizenId && e.DepartmentId == departmentId)
                .OrderByDescending(e => e.CreatedAtUtc)
                .ThenByDescending(e => e.AuditEventId)
                .ToListAsync(cancellationToken);

            return events.Select(MapToDto).ToList();
        }

        public Task LogCitizenStatusUpdatedAsync(
            int citizenId,
            int departmentId,
            string userId,
            string? userDisplayNameSnapshot,
            CitizenStatus newStatus,
            ShiftType? shiftType = null,
            CancellationToken cancellationToken = default)
        {
            var statusText = ToDanishStatus(newStatus);

            AddEvent(
                entityType: "Citizen",
                entityId: citizenId,
                action: "CitizenStatusUpdated",
                description: $"Borgerstatus ændret til {statusText}.",
                userId: userId,
                userDisplayNameSnapshot: userDisplayNameSnapshot,
                departmentId: departmentId,
                citizenId: citizenId,
                shiftType: shiftType);

            return Task.CompletedTask;
        }

        public Task LogFixedMedicationGivenAsync(
            int citizenId,
            int fixedMedicationId,
            string medicationName,
            int departmentId,
            string userId,
            string? userDisplayNameSnapshot,
            ShiftType? shiftType = null,
            CancellationToken cancellationToken = default)
        {
            AddEvent(
                entityType: "FixedMedication",
                entityId: fixedMedicationId,
                action: "FixedMedicationGiven",
                description: $"Fast medicin '{NormalizeDescriptionText(medicationName)}' registreret som givet.",
                userId: userId,
                userDisplayNameSnapshot: userDisplayNameSnapshot,
                departmentId: departmentId,
                citizenId: citizenId,
                shiftType: shiftType);

            return Task.CompletedTask;
        }

        public Task LogFixedMedicationCancelledAsync(
            int citizenId,
            int fixedMedicationId,
            string medicationName,
            int departmentId,
            string userId,
            string? userDisplayNameSnapshot,
            ShiftType? shiftType = null,
            CancellationToken cancellationToken = default)
        {
            AddEvent(
                entityType: "FixedMedication",
                entityId: fixedMedicationId,
                action: "FixedMedicationCancelled",
                description: $"Aktuel registrering for fast medicin '{NormalizeDescriptionText(medicationName)}' blev annulleret.",
                userId: userId,
                userDisplayNameSnapshot: userDisplayNameSnapshot,
                departmentId: departmentId,
                citizenId: citizenId,
                shiftType: shiftType);

            return Task.CompletedTask;
        }

        public Task LogPnMedicationRegisteredAsync(
            int citizenId,
            int pnMedicationId,
            string medicationName,
            int departmentId,
            string userId,
            string? userDisplayNameSnapshot,
            ShiftType shiftType,
            CancellationToken cancellationToken = default)
        {
            AddEvent(
                entityType: "PnMedication",
                entityId: pnMedicationId,
                action: "PnMedicationRegistered",
                description: $"PN-medicin '{NormalizeDescriptionText(medicationName)}' registreret.",
                userId: userId,
                userDisplayNameSnapshot: userDisplayNameSnapshot,
                departmentId: departmentId,
                citizenId: citizenId,
                shiftType: shiftType);

            return Task.CompletedTask;
        }

        private void AddEvent(
            string entityType,
            int entityId,
            string action,
            string description,
            string userId,
            string? userDisplayNameSnapshot,
            int departmentId,
            int citizenId,
            ShiftType? shiftType)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("Audit kræver en unik brugerreference.", nameof(userId));
            }

            var auditEvent = new AuditEvent
            {
                EntityType = NormalizeRequired(entityType, MaxEntityTypeLength, nameof(entityType)),
                EntityId = entityId,
                Action = NormalizeRequired(action, MaxActionLength, nameof(action)),
                Description = NormalizeRequired(description, MaxDescriptionLength, nameof(description)),
                UserId = userId.Trim(),
                UserDisplayNameSnapshot = NormalizeDisplayName(userDisplayNameSnapshot, userId),
                DepartmentId = departmentId,
                CitizenId = citizenId,
                ShiftType = shiftType,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.AuditEvents.Add(auditEvent);
        }

        private static AuditEventDto MapToDto(AuditEvent auditEvent)
        {
            return new AuditEventDto
            {
                AuditEventId = auditEvent.AuditEventId,
                EntityType = auditEvent.EntityType,
                EntityId = auditEvent.EntityId,
                Action = auditEvent.Action,
                Description = auditEvent.Description,
                UserId = auditEvent.UserId,
                UserDisplayNameSnapshot = auditEvent.UserDisplayNameSnapshot,
                DepartmentId = auditEvent.DepartmentId,
                CitizenId = auditEvent.CitizenId,
                ShiftType = auditEvent.ShiftType,
                ShiftDisplayName = auditEvent.ShiftType?.ToDanishDisplayName(),
                CreatedAtUtc = auditEvent.CreatedAtUtc
            };
        }

        private static string NormalizeRequired(string value, int maxLength, string parameterName)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException($"{parameterName} mangler.", parameterName);
            }

            if (normalized.Length > maxLength)
            {
                return normalized[..maxLength];
            }

            return normalized;
        }

        private static string NormalizeDisplayName(string? displayName, string userId)
        {
            var normalized = displayName?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = userId.Trim();
            }

            return normalized.Length > MaxUserDisplayNameLength
                ? normalized[..MaxUserDisplayNameLength]
                : normalized;
        }

        private static string NormalizeDescriptionText(string value)
        {
            var normalized = value?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(normalized) ? "ukendt" : normalized;
        }

        private static string ToDanishStatus(CitizenStatus status)
        {
            return status switch
            {
                CitizenStatus.Red => "rød",
                CitizenStatus.Yellow => "gul",
                CitizenStatus.Green => "grøn",
                _ => "ukendt"
            };
        }
    }
}
