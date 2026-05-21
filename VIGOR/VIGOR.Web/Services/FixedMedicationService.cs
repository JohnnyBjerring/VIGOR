using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Web.Data;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC04 service: hent fast medicin, registrer/ret aktuel givet-registrering, annuller aktuel registrering og ret medicinplan.
    /// Sikkerhed håndhæves ved at kræve, at borgeren tilhører den udledte afdeling.
    /// </summary>
    public class FixedMedicationService : IFixedMedicationService
    {
        private static readonly DateTime MinimumAcceptedGivenAtUtc = new(1999, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        private const int MaxMedicationNameLength = 100;
        private const int MaxScheduleDescriptionLength = 80;

        private readonly AppDbContext _context;
        private readonly IAuditService? _auditService;

        public FixedMedicationService(AppDbContext context, IAuditService? auditService = null)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IReadOnlyList<FixedMedicationDto>?> GetForCitizenAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            var citizenExists = await CitizenExistsInDepartmentAsync(citizenId, departmentId, cancellationToken);

            if (!citizenExists)
            {
                return null;
            }

            return await _context.FixedMedications
                .AsNoTracking()
                .Where(m => m.CitizenId == citizenId)
                .OrderByDescending(m => m.IsActive)
                .ThenBy(m => m.PlannedAt)
                .Select(m => new FixedMedicationDto
                {
                    FixedMedicationId = m.FixedMedicationId,
                    CitizenId = m.CitizenId,
                    Name = m.Name,
                    PlannedAt = m.PlannedAt,
                    ScheduleDescription = m.ScheduleDescription,
                    IsActive = m.IsActive,
                    IsGiven = m.IsGiven,
                    GivenAt = m.GivenAt,
                    GivenByUserId = m.GivenByUserId
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<FixedMedicationDto?> GiveAsync(
            int citizenId,
            int fixedMedicationId,
            int departmentId,
            string givenByUserId,
            DateTime? givenAt,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null)
        {
            if (string.IsNullOrWhiteSpace(givenByUserId))
            {
                throw new ArgumentException("GivenByUserId er påkrævet.", nameof(givenByUserId));
            }

            var medication = await FindMedicationForAccessibleCitizenAsync(citizenId, fixedMedicationId, departmentId, cancellationToken);
            if (medication == null)
            {
                return null;
            }

            if (!medication.IsActive)
            {
                throw new InvalidOperationException("Medicinplanen er inaktiv og kan ikke registreres som givet.");
            }

            var givenAtUtc = ResolveGivenAtUtc(givenAt);

            // UC04: den aktuelle registrering må kunne rettes/registreres igen. Fuld historik/audit håndteres senere i UC06.
            medication.IsGiven = true;
            medication.GivenAt = givenAtUtc;
            medication.GivenByUserId = givenByUserId;

            if (_auditService != null)
            {
                await _auditService.LogFixedMedicationGivenAsync(
                    citizenId,
                    fixedMedicationId,
                    medication.Name,
                    departmentId,
                    givenByUserId,
                    userDisplayNameSnapshot,
                    shiftType: null,
                    cancellationToken: cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return MapToDto(medication);
        }

        public async Task<FixedMedicationDto?> CancelGivenAsync(
            int citizenId,
            int fixedMedicationId,
            int departmentId,
            string cancelledByUserId,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null)
        {
            if (string.IsNullOrWhiteSpace(cancelledByUserId))
            {
                throw new ArgumentException("CancelledByUserId er påkrævet.", nameof(cancelledByUserId));
            }

            var medication = await FindMedicationForAccessibleCitizenAsync(citizenId, fixedMedicationId, departmentId, cancellationToken);
            if (medication == null)
            {
                return null;
            }

            // UC04 rydder kun den aktuelle registrering. UC06 opretter append-only audit-event for selve annulleringen.
            medication.IsGiven = false;
            medication.GivenAt = null;
            medication.GivenByUserId = null;

            if (_auditService != null)
            {
                await _auditService.LogFixedMedicationCancelledAsync(
                    citizenId,
                    fixedMedicationId,
                    medication.Name,
                    departmentId,
                    cancelledByUserId,
                    userDisplayNameSnapshot,
                    shiftType: null,
                    cancellationToken: cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return MapToDto(medication);
        }

        public async Task<FixedMedicationDto?> UpdatePlanAsync(
            int citizenId,
            int fixedMedicationId,
            int departmentId,
            UpdateFixedMedicationPlanRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentException("Medicinplanen mangler.", nameof(request));
            }

            var medication = await FindMedicationForAccessibleCitizenAsync(citizenId, fixedMedicationId, departmentId, cancellationToken);
            if (medication == null)
            {
                return null;
            }

            var normalizedName = NormalizeMedicationName(request.Name);
            var normalizedScheduleDescription = NormalizeScheduleDescription(request.ScheduleDescription);
            var normalizedPlannedAt = NormalizePlannedAt(request.PlannedAt);

            medication.Name = normalizedName;
            medication.PlannedAt = normalizedPlannedAt;
            medication.ScheduleDescription = normalizedScheduleDescription;
            medication.IsActive = request.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            return MapToDto(medication);
        }

        private async Task<VIGOR.Shared.Models.FixedMedication?> FindMedicationForAccessibleCitizenAsync(
            int citizenId,
            int fixedMedicationId,
            int departmentId,
            CancellationToken cancellationToken)
        {
            var citizenExists = await CitizenExistsInDepartmentAsync(citizenId, departmentId, cancellationToken);
            if (!citizenExists)
            {
                return null;
            }

            return await _context.FixedMedications
                .FirstOrDefaultAsync(m => m.FixedMedicationId == fixedMedicationId && m.CitizenId == citizenId, cancellationToken);
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

        private static DateTime ResolveGivenAtUtc(DateTime? givenAt)
        {
            if (!givenAt.HasValue)
            {
                return DateTime.UtcNow;
            }

            var value = givenAt.Value;
            if (value == default)
            {
                throw new ArgumentException(
                    "Tidspunktet for medicingivning er ugyldigt, fordi værdien er systemets minimumsværdi. Vælg dato og klokkeslæt igen, eller undlad tidspunkt for at bruge serverens nuværende tidspunkt.",
                    nameof(givenAt));
            }

            var utcValue = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
            };

            if (utcValue < MinimumAcceptedGivenAtUtc)
            {
                throw new ArgumentException(
                    "Tidspunktet for medicingivning er ugyldigt, fordi det ligger før 01-01-2000. Vælg dato og klokkeslæt igen, eller undlad tidspunkt for at bruge serverens nuværende tidspunkt.",
                    nameof(givenAt));
            }

            return DateTime.SpecifyKind(utcValue, DateTimeKind.Utc);
        }

        private static string NormalizeMedicationName(string name)
        {
            var normalized = name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException("Medicinens navn mangler. Skriv et navn, fx Panodil 1g.", nameof(name));
            }

            if (normalized.Length > MaxMedicationNameLength)
            {
                throw new ArgumentException($"Medicinens navn må højst være {MaxMedicationNameLength} tegn.", nameof(name));
            }

            return normalized;
        }

        private static string NormalizeScheduleDescription(string scheduleDescription)
        {
            var normalized = scheduleDescription?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return "Fast tidspunkt";
            }

            if (normalized.Length > MaxScheduleDescriptionLength)
            {
                throw new ArgumentException($"Planbeskrivelse må højst være {MaxScheduleDescriptionLength} tegn.", nameof(scheduleDescription));
            }

            return normalized;
        }

        private static DateTime NormalizePlannedAt(DateTime plannedAt)
        {
            if (plannedAt == default)
            {
                throw new ArgumentException("Planlagt tidspunkt mangler. Vælg et klokkeslæt.", nameof(plannedAt));
            }

            return new DateTime(2000, 1, 1, plannedAt.Hour, plannedAt.Minute, 0, DateTimeKind.Unspecified);
        }

        private static FixedMedicationDto MapToDto(VIGOR.Shared.Models.FixedMedication medication)
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
    }
}
