using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC09 service: oprettelse og visning af faglige noter for en borger.
    /// Sikkerhed håndhæves ved at kræve, at borgeren tilhører den udledte afdeling.
    /// </summary>
    public class NoteService : INoteService
    {
        private const int MaxContentLength = 1000;

        private readonly AppDbContext _context;
        private readonly IAuditService? _auditService;

        public NoteService(AppDbContext context, IAuditService? auditService = null)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IReadOnlyList<NoteDto>?> GetForCitizenAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            var citizenExists = await CitizenExistsInDepartmentAsync(citizenId, departmentId, cancellationToken);
            if (!citizenExists)
            {
                return null;
            }

            var notes = await _context.Notes
                .AsNoTracking()
                .Where(n => n.CitizenId == citizenId && n.DepartmentId == departmentId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .ThenByDescending(n => n.NoteId)
                .ToListAsync(cancellationToken);

            return notes
                .Select(MapToDto)
                .ToList();
        }

        public async Task<NoteDto?> CreateAsync(
            int citizenId,
            int departmentId,
            string createdByUserId,
            CreateNoteRequest request,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null)
        {
            if (request == null)
            {
                throw new ArgumentException("Noten mangler.", nameof(request));
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

            var content = NormalizeRequiredText(
                request.Content,
                MaxContentLength,
                "Noten mangler. Skriv en kort faglig note.",
                "Note");

            var note = new Note
            {
                CitizenId = citizenId,
                DepartmentId = departmentId,
                ShiftType = request.ShiftType,
                Content = content,
                CreatedByUserId = createdByUserId,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.Notes.Add(note);

            // Første SaveChanges opretter NoteId, som derefter bruges som EntityId i audit-eventet.
            await _context.SaveChangesAsync(cancellationToken);

            if (_auditService != null)
            {
                await _auditService.LogNoteCreatedAsync(
                    citizenId,
                    note.NoteId,
                    departmentId,
                    createdByUserId,
                    userDisplayNameSnapshot,
                    request.ShiftType,
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
            }

            return MapToDto(note);
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

        private static NoteDto MapToDto(Note note)
        {
            return new NoteDto
            {
                NoteId = note.NoteId,
                CitizenId = note.CitizenId,
                DepartmentId = note.DepartmentId,
                ShiftType = note.ShiftType,
                ShiftDisplayName = note.ShiftType.ToDanishDisplayName(),
                Content = note.Content,
                CreatedByUserId = note.CreatedByUserId,
                CreatedAtUtc = note.CreatedAtUtc
            };
        }
    }
}
