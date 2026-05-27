using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC10 service: oprettelse, visning og afslutning af opgaver for en borger.
    /// Sikkerhed håndhæves ved at kræve, at borgeren/opgaven tilhører den udledte afdeling.
    /// </summary>
    public class CitizenTaskService : ICitizenTaskService
    {
        private const int MaxTitleLength = 120;
        private const int MaxDescriptionLength = 1000;

        private readonly AppDbContext _context;
        private readonly IAuditService? _auditService;

        public CitizenTaskService(AppDbContext context, IAuditService? auditService = null)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IReadOnlyList<CitizenTaskDto>?> GetForCitizenAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            var citizenExists = await CitizenExistsInDepartmentAsync(citizenId, departmentId, cancellationToken);
            if (!citizenExists)
            {
                return null;
            }

            var tasks = await _context.CitizenTasks
                .AsNoTracking()
                .Where(t => t.CitizenId == citizenId && t.DepartmentId == departmentId)
                .OrderBy(t => t.IsCompleted)
                .ThenByDescending(t => t.CreatedAtUtc)
                .ThenByDescending(t => t.CitizenTaskId)
                .ToListAsync(cancellationToken);

            return tasks
                .Select(MapToDto)
                .ToList();
        }

        public async Task<CitizenTaskDto?> CreateAsync(
            int citizenId,
            int departmentId,
            string createdByUserId,
            CreateCitizenTaskRequest request,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null)
        {
            if (request == null)
            {
                throw new ArgumentException("Opgaven mangler.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(createdByUserId))
            {
                throw new ArgumentException("CreatedByUserId er påkrævet.", nameof(createdByUserId));
            }

            var citizenExists = await CitizenExistsInDepartmentAsync(citizenId, departmentId, cancellationToken);
            if (!citizenExists)
            {
                return null;
            }

            if (!Enum.IsDefined(typeof(ShiftType), request.ShiftType))
            {
                throw new ArgumentException("Vagttype er ugyldig. Vælg dagvagt, aftenvagt eller nattevagt.", nameof(request));
            }

            var title = NormalizeRequiredText(
                request.Title,
                MaxTitleLength,
                "Opgavens titel mangler. Skriv en kort opgavetitel.",
                "Titel");

            var description = NormalizeOptionalText(request.Description, MaxDescriptionLength, "Beskrivelse");

            var task = new CitizenTask
            {
                CitizenId = citizenId,
                DepartmentId = departmentId,
                ShiftType = request.ShiftType,
                Title = title,
                Description = description,
                CreatedByUserId = createdByUserId,
                CreatedAtUtc = DateTime.UtcNow,
                IsCompleted = false
            };

            _context.CitizenTasks.Add(task);

            // Første SaveChanges opretter CitizenTaskId, som derefter bruges som EntityId i audit-eventet.
            await _context.SaveChangesAsync(cancellationToken);

            if (_auditService != null)
            {
                await _auditService.LogCitizenTaskCreatedAsync(
                    citizenId,
                    task.CitizenTaskId,
                    task.Title,
                    departmentId,
                    createdByUserId,
                    userDisplayNameSnapshot,
                    request.ShiftType,
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
            }

            return MapToDto(task);
        }

        public async Task<CitizenTaskDto?> CompleteAsync(
            int citizenId,
            int citizenTaskId,
            int departmentId,
            string completedByUserId,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null)
        {
            if (string.IsNullOrWhiteSpace(completedByUserId))
            {
                throw new ArgumentException("CompletedByUserId er påkrævet.", nameof(completedByUserId));
            }

            var citizenExists = await CitizenExistsInDepartmentAsync(citizenId, departmentId, cancellationToken);
            if (!citizenExists)
            {
                return null;
            }

            var task = await _context.CitizenTasks
                .FirstOrDefaultAsync(t =>
                    t.CitizenTaskId == citizenTaskId &&
                    t.CitizenId == citizenId &&
                    t.DepartmentId == departmentId,
                    cancellationToken);

            if (task == null)
            {
                return null;
            }

            if (task.IsCompleted)
            {
                return MapToDto(task);
            }

            task.IsCompleted = true;
            task.CompletedAtUtc = DateTime.UtcNow;
            task.CompletedByUserId = completedByUserId;

            if (_auditService != null)
            {
                await _auditService.LogCitizenTaskCompletedAsync(
                    citizenId,
                    task.CitizenTaskId,
                    task.Title,
                    departmentId,
                    completedByUserId,
                    userDisplayNameSnapshot,
                    task.ShiftType,
                    cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return MapToDto(task);
        }

        private async Task<bool> CitizenExistsInDepartmentAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken)
        {
            return await _context.Citizens
                .AsNoTracking()
                .AnyAsync(c => c.CitizenId == citizenId && c.DepartmentId == departmentId, cancellationToken);
        }

        private static string NormalizeRequiredText(string value, int maxLength, string missingMessage, string label)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException(missingMessage);
            }

            if (normalized.Length > maxLength)
            {
                throw new ArgumentException($"{label} må højst være {maxLength} tegn.");
            }

            return normalized;
        }

        private static string NormalizeOptionalText(string? value, int maxLength, string label)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (normalized.Length > maxLength)
            {
                throw new ArgumentException($"{label} må højst være {maxLength} tegn.");
            }

            return normalized;
        }

        private static CitizenTaskDto MapToDto(CitizenTask task)
        {
            return new CitizenTaskDto
            {
                CitizenTaskId = task.CitizenTaskId,
                CitizenId = task.CitizenId,
                DepartmentId = task.DepartmentId,
                ShiftType = task.ShiftType,
                ShiftDisplayName = task.ShiftType.ToDanishDisplayName(),
                Title = task.Title,
                Description = task.Description,
                CreatedByUserId = task.CreatedByUserId,
                CreatedAtUtc = task.CreatedAtUtc,
                IsCompleted = task.IsCompleted,
                CompletedAtUtc = task.CompletedAtUtc,
                CompletedByUserId = task.CompletedByUserId
            };
        }
    }
}
