using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC11 service: tildeling og fjernelse af personale på en borger.
    /// Sikkerhed håndhæves ved at kræve, at både borger og tildelt medarbejder tilhører den udledte afdeling.
    /// </summary>
    public class StaffAssignmentService : IStaffAssignmentService
    {
        private readonly AppDbContext _context;
        private readonly IAuditService? _auditService;

        public StaffAssignmentService(AppDbContext context, IAuditService? auditService = null)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IReadOnlyList<CitizenStaffAssignmentDto>?> GetForCitizenAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            var citizenExists = await CitizenExistsInDepartmentAsync(citizenId, departmentId, cancellationToken);
            if (!citizenExists)
            {
                return null;
            }

            var assignments = await _context.CitizenStaffAssignments
                .AsNoTracking()
                .Where(a => a.CitizenId == citizenId && a.DepartmentId == departmentId)
                .OrderByDescending(a => a.IsActive)
                .ThenByDescending(a => a.AssignedAtUtc)
                .ThenByDescending(a => a.CitizenStaffAssignmentId)
                .ToListAsync(cancellationToken);

            var phoneLookup = await GetActivePhoneLookupAsync(assignments.Select(a => a.EmployeeId), cancellationToken);

            return assignments.Select(a => MapToDto(a, phoneLookup)).ToList();
        }

        public async Task<IReadOnlyList<AssignableStaffDto>?> GetAssignableStaffAsync(
            int citizenId,
            int departmentId,
            CancellationToken cancellationToken = default)
        {
            var citizenExists = await CitizenExistsInDepartmentAsync(citizenId, departmentId, cancellationToken);
            if (!citizenExists)
            {
                return null;
            }

            var employees = await _context.Employees
                .AsNoTracking()
                .Where(e => e.DepartmentId == departmentId)
                .OrderBy(e => e.Name)
                .ToListAsync(cancellationToken);

            var phoneLookup = await GetActivePhoneLookupAsync(employees.Select(e => e.EmployeeId), cancellationToken);

            return employees.Select(e => new AssignableStaffDto
            {
                EmployeeId = e.EmployeeId,
                Name = e.Name,
                DepartmentId = e.DepartmentId,
                ActivePhoneDisplayName = phoneLookup.TryGetValue(e.EmployeeId, out var assignment)
                    ? FormatPhoneDisplay(assignment.PhoneLabelSnapshot, assignment.PhoneNumberSnapshot)
                    : null
            }).ToList();
        }

        public async Task<CitizenStaffAssignmentDto?> AssignAsync(
            int citizenId,
            int departmentId,
            int employeeId,
            string assignedByUserId,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null)
        {
            if (employeeId <= 0)
            {
                throw new ArgumentException("Vælg en medarbejder, der skal tildeles borgeren.", nameof(employeeId));
            }

            if (string.IsNullOrWhiteSpace(assignedByUserId))
            {
                throw new ArgumentException("AssignedByUserId er påkrævet.", nameof(assignedByUserId));
            }

            var citizenExists = await CitizenExistsInDepartmentAsync(citizenId, departmentId, cancellationToken);
            if (!citizenExists)
            {
                return null;
            }

            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.DepartmentId == departmentId, cancellationToken);

            if (employee == null)
            {
                return null;
            }

            var existingActiveAssignment = await _context.CitizenStaffAssignments
                .FirstOrDefaultAsync(a =>
                    a.CitizenId == citizenId &&
                    a.DepartmentId == departmentId &&
                    a.EmployeeId == employeeId &&
                    a.IsActive,
                    cancellationToken);

            if (existingActiveAssignment != null)
            {
                var existingPhoneLookup = await GetActivePhoneLookupAsync(new[] { existingActiveAssignment.EmployeeId }, cancellationToken);
                return MapToDto(existingActiveAssignment, existingPhoneLookup);
            }

            var assignment = new CitizenStaffAssignment
            {
                CitizenId = citizenId,
                DepartmentId = departmentId,
                EmployeeId = employee.EmployeeId,
                EmployeeNameSnapshot = employee.Name,
                AssignedByUserId = assignedByUserId,
                AssignedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            _context.CitizenStaffAssignments.Add(assignment);

            // Første SaveChanges opretter CitizenStaffAssignmentId, som derefter bruges som EntityId i audit-eventet.
            await _context.SaveChangesAsync(cancellationToken);

            if (_auditService != null)
            {
                await _auditService.LogStaffAssignedToCitizenAsync(
                    citizenId,
                    assignment.CitizenStaffAssignmentId,
                    assignment.EmployeeNameSnapshot,
                    departmentId,
                    assignedByUserId,
                    userDisplayNameSnapshot,
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
            }

            var phoneLookup = await GetActivePhoneLookupAsync(new[] { assignment.EmployeeId }, cancellationToken);
            return MapToDto(assignment, phoneLookup);
        }

        public async Task<CitizenStaffAssignmentDto?> UnassignAsync(
            int citizenId,
            int citizenStaffAssignmentId,
            int departmentId,
            string unassignedByUserId,
            CancellationToken cancellationToken = default,
            string? userDisplayNameSnapshot = null)
        {
            if (string.IsNullOrWhiteSpace(unassignedByUserId))
            {
                throw new ArgumentException("UnassignedByUserId er påkrævet.", nameof(unassignedByUserId));
            }

            var citizenExists = await CitizenExistsInDepartmentAsync(citizenId, departmentId, cancellationToken);
            if (!citizenExists)
            {
                return null;
            }

            var assignment = await _context.CitizenStaffAssignments
                .FirstOrDefaultAsync(a =>
                    a.CitizenStaffAssignmentId == citizenStaffAssignmentId &&
                    a.CitizenId == citizenId &&
                    a.DepartmentId == departmentId,
                    cancellationToken);

            if (assignment == null)
            {
                return null;
            }

            if (!assignment.IsActive)
            {
                var existingPhoneLookup = await GetActivePhoneLookupAsync(new[] { assignment.EmployeeId }, cancellationToken);
                return MapToDto(assignment, existingPhoneLookup);
            }

            assignment.IsActive = false;
            assignment.UnassignedAtUtc = DateTime.UtcNow;
            assignment.UnassignedByUserId = unassignedByUserId;

            if (_auditService != null)
            {
                await _auditService.LogStaffUnassignedFromCitizenAsync(
                    citizenId,
                    assignment.CitizenStaffAssignmentId,
                    assignment.EmployeeNameSnapshot,
                    departmentId,
                    unassignedByUserId,
                    userDisplayNameSnapshot,
                    cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            var phoneLookup = await GetActivePhoneLookupAsync(new[] { assignment.EmployeeId }, cancellationToken);
            return MapToDto(assignment, phoneLookup);
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

        private async Task<Dictionary<int, PhoneAssignment>> GetActivePhoneLookupAsync(
            IEnumerable<int> employeeIds,
            CancellationToken cancellationToken)
        {
            var ids = employeeIds.Distinct().ToArray();
            if (ids.Length == 0)
            {
                return new Dictionary<int, PhoneAssignment>();
            }

            var assignments = await _context.PhoneAssignments
                .AsNoTracking()
                .Where(a => a.IsActive && ids.Contains(a.EmployeeId))
                .ToListAsync(cancellationToken);

            return assignments
                .GroupBy(a => a.EmployeeId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AssignedAtUtc).First());
        }

        private static CitizenStaffAssignmentDto MapToDto(
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
    }
}
