using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC08: Samler eksisterende runtime-data til en enkel overlapvisning.
    /// Servicen opretter ikke nye data; den læser borgere, medicin, PN, audit, noter og åbne opgaver for brugerens afdeling.
    /// </summary>
    public class OverlapService : IOverlapService
    {
        private const int MaxRecentPnMedications = 3;
        private const int MaxRecentAuditEvents = 5;
        private const int MaxRecentNotes = 3;

        private readonly AppDbContext _context;

        public OverlapService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OverlapDto?> GetOverlapAsync(
            int departmentId,
            ShiftType? activeShiftType = null,
            CancellationToken cancellationToken = default)
        {
            var department = await _context.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DepartmentId == departmentId, cancellationToken);

            if (department == null)
            {
                return null;
            }

            var citizens = await _context.Citizens
                .AsNoTracking()
                .Where(c => c.DepartmentId == departmentId)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            var citizenDtos = new List<CitizenOverlapDto>();

            foreach (var citizen in citizens)
            {
                var fixedMedications = await _context.FixedMedications
                    .AsNoTracking()
                    .Where(m => m.CitizenId == citizen.CitizenId)
                    .OrderBy(m => m.PlannedAt)
                    .ThenBy(m => m.Name)
                    .ToListAsync(cancellationToken);

                var recentPnMedications = await _context.PnMedications
                    .AsNoTracking()
                    .Where(m => m.CitizenId == citizen.CitizenId && m.DepartmentId == departmentId)
                    .OrderByDescending(m => m.GivenAtUtc)
                    .ThenByDescending(m => m.PnMedicationId)
                    .Take(MaxRecentPnMedications)
                    .ToListAsync(cancellationToken);

                var recentAuditEvents = await _context.AuditEvents
                    .AsNoTracking()
                    .Where(e => e.CitizenId == citizen.CitizenId && e.DepartmentId == departmentId)
                    .OrderByDescending(e => e.CreatedAtUtc)
                    .ThenByDescending(e => e.AuditEventId)
                    .Take(MaxRecentAuditEvents)
                    .ToListAsync(cancellationToken);

                var recentNotes = await _context.Notes
                    .AsNoTracking()
                    .Where(n => n.CitizenId == citizen.CitizenId && n.DepartmentId == departmentId)
                    .OrderByDescending(n => n.CreatedAtUtc)
                    .ThenByDescending(n => n.NoteId)
                    .Take(MaxRecentNotes)
                    .ToListAsync(cancellationToken);

                var openTasks = await _context.CitizenTasks
                    .AsNoTracking()
                    .Where(t => t.CitizenId == citizen.CitizenId && t.DepartmentId == departmentId && !t.IsCompleted)
                    .OrderByDescending(t => t.CreatedAtUtc)
                    .ThenByDescending(t => t.CitizenTaskId)
                    .ToListAsync(cancellationToken);

                var activeStaffAssignments = await _context.CitizenStaffAssignments
                    .AsNoTracking()
                    .Where(a => a.CitizenId == citizen.CitizenId && a.DepartmentId == departmentId && a.IsActive)
                    .OrderBy(a => a.EmployeeNameSnapshot)
                    .ThenByDescending(a => a.AssignedAtUtc)
                    .ToListAsync(cancellationToken);

                var activeStaffEmployeeIds = activeStaffAssignments.Select(a => a.EmployeeId).Distinct().ToArray();
                var activePhoneAssignments = activeStaffEmployeeIds.Length == 0
                    ? new List<PhoneAssignment>()
                    : await _context.PhoneAssignments
                        .AsNoTracking()
                        .Where(a => a.IsActive && activeStaffEmployeeIds.Contains(a.EmployeeId))
                        .ToListAsync(cancellationToken);

                var phoneLookup = activePhoneAssignments
                    .GroupBy(a => a.EmployeeId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AssignedAtUtc).First());

                citizenDtos.Add(new CitizenOverlapDto
                {
                    CitizenId = citizen.CitizenId,
                    Name = citizen.Name,
                    Status = citizen.Status,
                    StatusDisplayName = ToDanishStatus(citizen.Status),
                    FixedMedications = fixedMedications.Select(MapFixedMedication).ToList(),
                    RecentPnMedications = recentPnMedications.Select(MapPnMedication).ToList(),
                    RecentAuditEvents = recentAuditEvents.Select(MapAuditEvent).ToList(),
                    ActiveNotes = recentNotes.Select(MapNote).ToList(),
                    OpenTasks = openTasks.Select(MapTask).ToList(),
                    ActiveStaffAssignments = activeStaffAssignments.Select(a => MapStaffAssignment(a, phoneLookup)).ToList(),
                    FixedMedicationCount = fixedMedications.Count,
                    RecentPnMedicationCount = recentPnMedications.Count,
                    RecentAuditEventCount = recentAuditEvents.Count,
                    ActiveNoteCount = recentNotes.Count,
                    OpenTaskCount = openTasks.Count,
                    ActiveStaffAssignmentCount = activeStaffAssignments.Count
                });
            }

            return new OverlapDto
            {
                DepartmentId = department.DepartmentId,
                DepartmentName = department.Name,
                ActiveShiftType = activeShiftType,
                ActiveShiftDisplayName = activeShiftType?.ToDanishDisplayName(),
                GeneratedAtUtc = DateTime.UtcNow,
                Citizens = citizenDtos
            };
        }

        private static FixedMedicationDto MapFixedMedication(FixedMedication medication)
        {
            return new FixedMedicationDto
            {
                FixedMedicationId = medication.FixedMedicationId,
                CitizenId = medication.CitizenId,
                Name = medication.Name,
                PlannedAt = medication.PlannedAt,
                ScheduleDescription = medication.ScheduleDescription,
                IsActive = medication.IsActive,
                IsGiven = medication.IsGiven,
                GivenAt = medication.GivenAt,
                GivenByUserId = medication.GivenByUserId
            };
        }

        private static PnMedicationDto MapPnMedication(PnMedication medication)
        {
            return new PnMedicationDto
            {
                PnMedicationId = medication.PnMedicationId,
                CitizenId = medication.CitizenId,
                DepartmentId = medication.DepartmentId,
                ShiftType = medication.ShiftType,
                ShiftDisplayName = medication.ShiftType.ToDanishDisplayName(),
                MedicineName = medication.MedicineName,
                Dose = medication.Dose,
                Reason = medication.Reason,
                GivenAtUtc = medication.GivenAtUtc,
                GivenByUserId = medication.GivenByUserId,
                CreatedAtUtc = medication.CreatedAtUtc
            };
        }

        private static AuditEventDto MapAuditEvent(AuditEvent auditEvent)
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

        private static NoteDto MapNote(Note note)
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

        private static CitizenTaskDto MapTask(CitizenTask task)
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


        private static CitizenStaffAssignmentDto MapStaffAssignment(
            CitizenStaffAssignment assignment,
            IReadOnlyDictionary<int, PhoneAssignment>? activePhoneAssignments = null)
        {
            PhoneAssignment? phoneAssignment = null;
            if (activePhoneAssignments != null)
            {
                activePhoneAssignments.TryGetValue(assignment.EmployeeId, out phoneAssignment);
            }

            return new CitizenStaffAssignmentDto
            {
                CitizenStaffAssignmentId = assignment.CitizenStaffAssignmentId,
                CitizenId = assignment.CitizenId,
                DepartmentId = assignment.DepartmentId,
                EmployeeId = assignment.EmployeeId,
                EmployeeNameSnapshot = assignment.EmployeeNameSnapshot,
                ActivePhoneLabel = phoneAssignment?.PhoneLabelSnapshot,
                ActivePhoneNumber = phoneAssignment?.PhoneNumberSnapshot,
                ActivePhoneDisplayName = phoneAssignment == null
                    ? null
                    : FormatPhoneDisplay(phoneAssignment.PhoneLabelSnapshot, phoneAssignment.PhoneNumberSnapshot),
                AssignedByUserId = assignment.AssignedByUserId,
                AssignedAtUtc = assignment.AssignedAtUtc,
                IsActive = assignment.IsActive,
                UnassignedAtUtc = assignment.UnassignedAtUtc,
                UnassignedByUserId = assignment.UnassignedByUserId
            };
        }

        private static string FormatPhoneDisplay(string? label, string number)
        {
            return string.IsNullOrWhiteSpace(label)
                ? number
                : $"{label} ({number})";
        }

        private static string ToDanishStatus(CitizenStatus status)
        {
            return status switch
            {
                CitizenStatus.Red => "Rød",
                CitizenStatus.Yellow => "Gul",
                CitizenStatus.Green => "Grøn",
                _ => "Ukendt"
            };
        }
    }
}
