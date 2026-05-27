using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC12 service: opretter arbejdstelefoner og håndterer aktiv tildeling til medarbejdere.
    /// En telefon kan kun have én aktiv medarbejder, og en medarbejder kan kun have én aktiv telefon.
    /// </summary>
    public class WorkPhoneService : IWorkPhoneService
    {
        private const int MaxLabelLength = 100;
        private const int MaxPhoneNumberLength = 40;
        private const int MaxEmployeeNameSnapshotLength = 200;
        private const int MaxUserIdLength = 450;

        private readonly AppDbContext _context;

        public WorkPhoneService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<WorkPhoneDto>> GetPhonesAsync(CancellationToken cancellationToken = default)
        {
            var phones = await _context.WorkPhones
                .AsNoTracking()
                .OrderBy(p => p.Label)
                .ThenBy(p => p.PhoneNumber)
                .ToListAsync(cancellationToken);

            var activeAssignments = await _context.PhoneAssignments
                .AsNoTracking()
                .Where(a => a.IsActive)
                .ToListAsync(cancellationToken);

            var byPhoneId = activeAssignments
                .GroupBy(a => a.WorkPhoneId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AssignedAtUtc).First());

            return phones.Select(phone => MapPhone(phone, byPhoneId.GetValueOrDefault(phone.WorkPhoneId))).ToList();
        }

        public async Task<IReadOnlyList<PhoneAssignmentDto>> GetActiveAssignmentsAsync(CancellationToken cancellationToken = default)
        {
            var assignments = await _context.PhoneAssignments
                .AsNoTracking()
                .Where(a => a.IsActive)
                .OrderBy(a => a.EmployeeNameSnapshot)
                .ThenBy(a => a.PhoneLabelSnapshot)
                .ToListAsync(cancellationToken);

            return assignments.Select(MapAssignment).ToList();
        }

        public async Task<IReadOnlyList<PhoneAssignableEmployeeDto>> GetAssignableEmployeesAsync(CancellationToken cancellationToken = default)
        {
            var employees = await _context.Employees
                .AsNoTracking()
                .OrderBy(e => e.Name)
                .ToListAsync(cancellationToken);

            var departments = await _context.Departments
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var activeAssignments = await _context.PhoneAssignments
                .AsNoTracking()
                .Where(a => a.IsActive)
                .ToListAsync(cancellationToken);

            var byEmployeeId = activeAssignments
                .GroupBy(a => a.EmployeeId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AssignedAtUtc).First());

            return employees.Select(employee =>
            {
                var activeAssignment = byEmployeeId.GetValueOrDefault(employee.EmployeeId);
                var department = employee.DepartmentId == null
                    ? null
                    : departments.FirstOrDefault(d => d.DepartmentId == employee.DepartmentId.Value);

                return new PhoneAssignableEmployeeDto
                {
                    EmployeeId = employee.EmployeeId,
                    Name = employee.Name,
                    DepartmentId = employee.DepartmentId,
                    DepartmentName = department?.Name,
                    ActivePhoneDisplayName = activeAssignment == null ? null : FormatPhoneDisplay(activeAssignment.PhoneLabelSnapshot, activeAssignment.PhoneNumberSnapshot)
                };
            }).ToList();
        }

        public async Task<WorkPhoneDto> CreatePhoneAsync(CreateWorkPhoneRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var phoneNumber = NormalizeRequired(request.PhoneNumber, MaxPhoneNumberLength, nameof(request.PhoneNumber));
            var label = NormalizeOptional(request.Label, MaxLabelLength);
            if (string.IsNullOrWhiteSpace(label))
            {
                label = phoneNumber;
            }

            var phoneExists = await _context.WorkPhones
                .AsNoTracking()
                .AnyAsync(p => p.PhoneNumber == phoneNumber, cancellationToken);

            if (phoneExists)
            {
                throw new InvalidOperationException("Der findes allerede en arbejdstelefon med dette nummer.");
            }

            var phone = new WorkPhone
            {
                Label = label,
                PhoneNumber = phoneNumber,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.WorkPhones.Add(phone);
            await _context.SaveChangesAsync(cancellationToken);

            return MapPhone(phone, activeAssignment: null);
        }

        public async Task<PhoneAssignmentDto?> AssignPhoneAsync(
            AssignWorkPhoneRequest request,
            string assignedByUserId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.WorkPhoneId <= 0)
            {
                throw new ArgumentException("Vælg en arbejdstelefon.", nameof(request.WorkPhoneId));
            }

            if (request.EmployeeId <= 0)
            {
                throw new ArgumentException("Vælg en medarbejder.", nameof(request.EmployeeId));
            }

            var userId = NormalizeRequired(assignedByUserId, MaxUserIdLength, nameof(assignedByUserId));

            var phone = await _context.WorkPhones
                .FirstOrDefaultAsync(p => p.WorkPhoneId == request.WorkPhoneId && p.IsActive, cancellationToken);

            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId, cancellationToken);

            if (phone == null || employee == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;

            var existingActiveAssignments = await _context.PhoneAssignments
                .Where(a => a.IsActive && (a.WorkPhoneId == phone.WorkPhoneId || a.EmployeeId == employee.EmployeeId))
                .ToListAsync(cancellationToken);

            foreach (var existing in existingActiveAssignments)
            {
                existing.IsActive = false;
                existing.UnassignedAtUtc = now;
                existing.UnassignedByUserId = userId;
            }

            var assignment = new PhoneAssignment
            {
                WorkPhoneId = phone.WorkPhoneId,
                EmployeeId = employee.EmployeeId,
                DepartmentId = employee.DepartmentId,
                EmployeeNameSnapshot = NormalizeRequired(employee.Name, MaxEmployeeNameSnapshotLength, nameof(employee.Name)),
                PhoneLabelSnapshot = NormalizeOptional(phone.Label, MaxLabelLength),
                PhoneNumberSnapshot = NormalizeRequired(phone.PhoneNumber, MaxPhoneNumberLength, nameof(phone.PhoneNumber)),
                AssignedByUserId = userId,
                AssignedAtUtc = now,
                IsActive = true
            };

            _context.PhoneAssignments.Add(assignment);
            await _context.SaveChangesAsync(cancellationToken);

            return MapAssignment(assignment);
        }

        public async Task<PhoneAssignmentDto?> UnassignPhoneAsync(
            int phoneAssignmentId,
            string unassignedByUserId,
            CancellationToken cancellationToken = default)
        {
            if (phoneAssignmentId <= 0)
            {
                throw new ArgumentException("Telefontildelingen er ugyldig.", nameof(phoneAssignmentId));
            }

            var userId = NormalizeRequired(unassignedByUserId, MaxUserIdLength, nameof(unassignedByUserId));

            var assignment = await _context.PhoneAssignments
                .FirstOrDefaultAsync(a => a.PhoneAssignmentId == phoneAssignmentId, cancellationToken);

            if (assignment == null)
            {
                return null;
            }

            if (!assignment.IsActive)
            {
                return MapAssignment(assignment);
            }

            assignment.IsActive = false;
            assignment.UnassignedAtUtc = DateTime.UtcNow;
            assignment.UnassignedByUserId = userId;

            await _context.SaveChangesAsync(cancellationToken);

            return MapAssignment(assignment);
        }

        private static WorkPhoneDto MapPhone(WorkPhone phone, PhoneAssignment? activeAssignment)
        {
            return new WorkPhoneDto
            {
                WorkPhoneId = phone.WorkPhoneId,
                Label = phone.Label,
                PhoneNumber = phone.PhoneNumber,
                IsActive = phone.IsActive,
                CreatedAtUtc = phone.CreatedAtUtc,
                IsAssigned = activeAssignment != null,
                ActivePhoneAssignmentId = activeAssignment?.PhoneAssignmentId,
                AssignedEmployeeId = activeAssignment?.EmployeeId,
                AssignedEmployeeName = activeAssignment?.EmployeeNameSnapshot,
                AssignedDepartmentId = activeAssignment?.DepartmentId,
                AssignedAtUtc = activeAssignment?.AssignedAtUtc
            };
        }

        private static PhoneAssignmentDto MapAssignment(PhoneAssignment assignment)
        {
            return new PhoneAssignmentDto
            {
                PhoneAssignmentId = assignment.PhoneAssignmentId,
                WorkPhoneId = assignment.WorkPhoneId,
                PhoneLabelSnapshot = assignment.PhoneLabelSnapshot,
                PhoneNumberSnapshot = assignment.PhoneNumberSnapshot,
                EmployeeId = assignment.EmployeeId,
                EmployeeNameSnapshot = assignment.EmployeeNameSnapshot,
                DepartmentId = assignment.DepartmentId,
                AssignedByUserId = assignment.AssignedByUserId,
                AssignedAtUtc = assignment.AssignedAtUtc,
                IsActive = assignment.IsActive,
                UnassignedAtUtc = assignment.UnassignedAtUtc,
                UnassignedByUserId = assignment.UnassignedByUserId
            };
        }

        private static string NormalizeRequired(string? value, int maxLength, string parameterName)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException($"{parameterName} mangler.", parameterName);
            }

            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }

        private static string NormalizeOptional(string? value, int maxLength)
        {
            var normalized = value?.Trim() ?? string.Empty;
            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }

        private static string FormatPhoneDisplay(string? label, string number)
        {
            return string.IsNullOrWhiteSpace(label)
                ? number
                : $"{label} ({number})";
        }
    }
}
