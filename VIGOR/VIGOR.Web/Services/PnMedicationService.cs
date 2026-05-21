using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC05 service: registrering og visning af PN-medicin givet ved behov.
    /// Sikkerhed håndhæves ved at kræve, at borgeren tilhører den udledte afdeling.
    /// </summary>
    public class PnMedicationService : IPnMedicationService
    {
        private static readonly DateTime MinimumAcceptedGivenAtUtc = new(1999, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        private const int MaxMedicineNameLength = 100;
        private const int MaxDoseLength = 60;
        private const int MaxReasonLength = 250;

        private readonly AppDbContext _context;
        private readonly IAuditService? _auditService;

        public PnMedicationService(AppDbContext context, IAuditService? auditService = null)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IReadOnlyList<PnMedicationDto>?> GetForCitizenAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            var citizenExists = await CitizenExistsInDepartmentAsync(citizenId, departmentId, cancellationToken);
            if (!citizenExists)
            {
                return null;
            }

            var registrations = await _context.PnMedications
                .AsNoTracking()
                .Where(m => m.CitizenId == citizenId && m.DepartmentId == departmentId)
                .OrderByDescending(m => m.GivenAtUtc)
                .ThenByDescending(m => m.PnMedicationId)
                .ToListAsync(cancellationToken);

            return registrations
                .Select(MapToDto)
                .ToList();
        }

        public async Task<PnMedicationDto?> RegisterAsync(
            int citizenId,
            int departmentId,
            string givenByUserId,
            RegisterPnMedicationRequest request,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null)
        {
            if (request == null)
            {
                throw new ArgumentException("PN-medicinregistreringen mangler.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(givenByUserId))
            {
                throw new ArgumentException("GivenByUserId er påkrævet.", nameof(givenByUserId));
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

            var medicineName = NormalizeRequiredText(request.MedicineName, MaxMedicineNameLength, "Medicinens navn mangler. Skriv et navn, fx Panodil.", "Medicinens navn");
            var dose = NormalizeRequiredText(request.Dose, MaxDoseLength, "Dosis mangler. Skriv dosis, fx 1 tablet eller 5 mg.", "Dosis");
            var reason = NormalizeRequiredText(request.Reason, MaxReasonLength, "Årsag/behov mangler. Skriv kort hvorfor PN-medicinen gives.", "Årsag/behov");
            var givenAtUtc = ResolveGivenAtUtc(request.GivenAt);
            var nowUtc = DateTime.UtcNow;

            var medication = new PnMedication
            {
                CitizenId = citizenId,
                DepartmentId = departmentId,
                ShiftType = request.ShiftType,
                MedicineName = medicineName,
                Dose = dose,
                Reason = reason,
                GivenAtUtc = givenAtUtc,
                GivenByUserId = givenByUserId,
                CreatedAtUtc = nowUtc
            };

            _context.PnMedications.Add(medication);

            // Første SaveChanges opretter PnMedicationId, som derefter bruges som EntityId i audit-eventet.
            await _context.SaveChangesAsync(cancellationToken);

            if (_auditService != null)
            {
                await _auditService.LogPnMedicationRegisteredAsync(
                    citizenId,
                    medication.PnMedicationId,
                    medicineName,
                    departmentId,
                    givenByUserId,
                    userDisplayNameSnapshot,
                    request.ShiftType,
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
            }

            return MapToDto(medication);
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
                    "Tidspunktet for PN-medicin er ugyldigt, fordi værdien er systemets minimumsværdi. Vælg dato og klokkeslæt igen, eller undlad tidspunkt for at bruge serverens nuværende tidspunkt.",
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
                    "Tidspunktet for PN-medicin er ugyldigt, fordi det ligger før 01-01-2000. Vælg dato og klokkeslæt igen, eller undlad tidspunkt for at bruge serverens nuværende tidspunkt.",
                    nameof(givenAt));
            }

            return DateTime.SpecifyKind(utcValue, DateTimeKind.Utc);
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

        private static PnMedicationDto MapToDto(PnMedication medication)
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
    }
}
