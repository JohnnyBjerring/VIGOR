using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// UC13 service: smal brugeradministration oven på ASP.NET Identity.
    /// Scope er bevidst begrænset til vis brugere, opret bruger, ændr rolle og aktiver/deaktiver bruger.
    /// </summary>
    public class UserAdminService : IUserAdminService
    {
        private const int MaxDisplayNameLength = 200;
        private const int MaxEmailLength = 256;
        private const int MinimumPasswordLength = 4;

        private static readonly IReadOnlyDictionary<string, int> RoleLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Personale"] = 10,
            ["Vagtansvarlig"] = 20,
            ["Leder"] = 30,
            ["Superbruger"] = 40
        };

        private readonly AppDbContext _context;
        private readonly IPasswordHasher<IdentityUser> _passwordHasher;

        public UserAdminService(AppDbContext context, IPasswordHasher<IdentityUser> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<IReadOnlyList<UserAdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            var users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.Email)
                .ToListAsync(cancellationToken);

            var employees = await _context.Employees
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var departments = await _context.Departments
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var userRoles = await _context.UserRoles
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var roles = await _context.Roles
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var phoneAssignments = await _context.PhoneAssignments
                .AsNoTracking()
                .Where(a => a.IsActive)
                .ToListAsync(cancellationToken);

            return users
                .Select(user => MapToDto(user, employees, departments, userRoles, roles, phoneAssignments))
                .ToList();
        }

        public async Task<IReadOnlyList<UserAdminRoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            var roles = await _context.Roles
                .AsNoTracking()
                .Select(r => new UserAdminRoleDto { Name = r.Name ?? string.Empty })
                .ToListAsync(cancellationToken);

            return roles
                .Where(r => GetRoleLevel(r.Name) > 0)
                .OrderByDescending(r => GetRoleLevel(r.Name))
                .ThenBy(r => r.Name)
                .ToList();
        }

        public async Task<IReadOnlyList<UserAdminDepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .Select(d => new UserAdminDepartmentDto
                {
                    DepartmentId = d.DepartmentId,
                    Name = d.Name
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<UserAdminUserDto> CreateUserAsync(
            CreateUserAdminUserRequest request,
            IReadOnlyCollection<string> actorRoleNames,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var email = NormalizeRequired(request.Email, MaxEmailLength, nameof(request.Email));
            var normalizedEmail = NormalizeKey(email);
            var displayName = NormalizeDisplayName(request.DisplayName, email);
            var role = await FindRoleByNameAsync(request.RoleName, cancellationToken);

            if (role == null)
            {
                throw new ArgumentException("Den valgte rolle findes ikke.", nameof(request.RoleName));
            }

            EnsureActorCanAssignRole(request.RoleName, actorRoleNames);

            if (request.DepartmentId == null || request.DepartmentId <= 0)
            {
                throw new ArgumentException("Vælg en afdeling til brugeren.", nameof(request.DepartmentId));
            }

            var departmentExists = await _context.Departments
                .AnyAsync(d => d.DepartmentId == request.DepartmentId.Value, cancellationToken);

            if (!departmentExists)
            {
                throw new ArgumentException("Den valgte afdeling findes ikke.", nameof(request.DepartmentId));
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < MinimumPasswordLength)
            {
                throw new ArgumentException($"Adgangskoden skal være mindst {MinimumPasswordLength} tegn.", nameof(request.Password));
            }

            var emailInUse = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

            if (emailInUse)
            {
                throw new InvalidOperationException("Der findes allerede en bruger med den email.");
            }

            var user = new IdentityUser
            {
                UserName = email,
                NormalizedUserName = normalizedEmail,
                Email = email,
                NormalizedEmail = normalizedEmail,
                EmailConfirmed = true,
                LockoutEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString("D"),
                ConcurrencyStamp = Guid.NewGuid().ToString("D")
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            _context.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            _context.Employees.Add(new Employee
            {
                IdentityUserId = user.Id,
                Name = displayName,
                DepartmentId = request.DepartmentId.Value
            });

            await _context.SaveChangesAsync(cancellationToken);

            return (await GetUserByIdAsync(user.Id, cancellationToken))!;
        }

        public async Task<UserAdminUserDto?> UpdateRoleAsync(
            string userId,
            UpdateUserRoleRequest request,
            string actorUserId,
            IReadOnlyCollection<string> actorRoleNames,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(actorUserId))
            {
                throw new ArgumentException("Den aktuelle bruger kunne ikke identificeres.", nameof(actorUserId));
            }

            if (string.Equals(userId, actorUserId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Du kan ikke ændre rollen på den bruger, du selv er logget ind med.");
            }

            await EnsureActorCanManageTargetUserAsync(userId, actorRoleNames, cancellationToken);
            EnsureActorCanAssignRole(request.RoleName, actorRoleNames);

            var role = await FindRoleByNameAsync(request.RoleName, cancellationToken);
            if (role == null)
            {
                throw new ArgumentException("Den valgte rolle findes ikke.", nameof(request.RoleName));
            }

            var existingUserRoles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .ToListAsync(cancellationToken);

            _context.UserRoles.RemoveRange(existingUserRoles);
            _context.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            user.SecurityStamp = Guid.NewGuid().ToString("D");
            user.ConcurrencyStamp = Guid.NewGuid().ToString("D");

            await _context.SaveChangesAsync(cancellationToken);

            return await GetUserByIdAsync(user.Id, cancellationToken);
        }

        public async Task<UserAdminUserDto?> SetActiveAsync(
            string userId,
            SetUserActiveRequest request,
            string actorUserId,
            IReadOnlyCollection<string> actorRoleNames,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(actorUserId))
            {
                throw new ArgumentException("Den aktuelle bruger kunne ikke identificeres.", nameof(actorUserId));
            }

            if (!request.IsActive && string.Equals(userId, actorUserId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Du kan ikke deaktivere den bruger, du selv er logget ind med.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
            {
                return null;
            }

            await EnsureActorCanManageTargetUserAsync(userId, actorRoleNames, cancellationToken);

            user.LockoutEnabled = true;
            user.LockoutEnd = request.IsActive ? null : DateTimeOffset.MaxValue;
            user.AccessFailedCount = 0;
            user.SecurityStamp = Guid.NewGuid().ToString("D");
            user.ConcurrencyStamp = Guid.NewGuid().ToString("D");

            await _context.SaveChangesAsync(cancellationToken);

            return await GetUserByIdAsync(user.Id, cancellationToken);
        }


        private async Task EnsureActorCanManageTargetUserAsync(
            string targetUserId,
            IReadOnlyCollection<string> actorRoleNames,
            CancellationToken cancellationToken)
        {
            var actorLevel = GetHighestRoleLevel(actorRoleNames);
            if (actorLevel <= 0)
            {
                throw new UnauthorizedAccessException("Den aktuelle bruger har ikke en rolle, der må administrere brugere.");
            }

            var targetRoles = await GetRoleNamesForUserAsync(targetUserId, cancellationToken);
            var targetLevel = GetHighestRoleLevel(targetRoles);

            if (targetLevel > actorLevel)
            {
                throw new InvalidOperationException("Du kan ikke ændre en bruger med højere rolle end din egen.");
            }
        }

        private static void EnsureActorCanAssignRole(string roleName, IReadOnlyCollection<string> actorRoleNames)
        {
            var actorLevel = GetHighestRoleLevel(actorRoleNames);
            if (actorLevel <= 0)
            {
                throw new UnauthorizedAccessException("Den aktuelle bruger har ikke en rolle, der må administrere brugere.");
            }

            var requestedLevel = GetRoleLevel(roleName);
            if (requestedLevel <= 0)
            {
                throw new ArgumentException("Den valgte rolle findes ikke i systemets rollehierarki.", nameof(roleName));
            }

            if (requestedLevel > actorLevel)
            {
                throw new InvalidOperationException("Du kan ikke tildele en rolle, der er højere end din egen rolle.");
            }
        }

        private async Task<IReadOnlyCollection<string>> GetRoleNamesForUserAsync(string userId, CancellationToken cancellationToken)
        {
            return await (from userRole in _context.UserRoles.AsNoTracking()
                          join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                          where userRole.UserId == userId
                          select role.Name ?? string.Empty)
                .Where(roleName => roleName != string.Empty)
                .ToListAsync(cancellationToken);
        }

        private static int GetHighestRoleLevel(IEnumerable<string>? roleNames)
        {
            return roleNames?.Select(GetRoleLevel).DefaultIfEmpty(0).Max() ?? 0;
        }

        private static int GetRoleLevel(string? roleName)
        {
            return !string.IsNullOrWhiteSpace(roleName) && RoleLevels.TryGetValue(roleName.Trim(), out var level)
                ? level
                : 0;
        }

        private async Task<UserAdminUserDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken)
        {
            var users = await GetUsersAsync(cancellationToken);
            return users.FirstOrDefault(u => u.UserId == userId);
        }

        private async Task<IdentityRole?> FindRoleByNameAsync(string? roleName, CancellationToken cancellationToken)
        {
            var normalizedRoleName = NormalizeKey(roleName);
            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                return null;
            }

            return await _context.Roles
                .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName || r.Name == roleName, cancellationToken);
        }

        private static UserAdminUserDto MapToDto(
            IdentityUser user,
            IReadOnlyCollection<Employee> employees,
            IReadOnlyCollection<Department> departments,
            IReadOnlyCollection<IdentityUserRole<string>> userRoles,
            IReadOnlyCollection<IdentityRole> roles,
            IReadOnlyCollection<PhoneAssignment> phoneAssignments)
        {
            var employee = employees.FirstOrDefault(e => e.IdentityUserId == user.Id);
            var department = employee?.DepartmentId == null
                ? null
                : departments.FirstOrDefault(d => d.DepartmentId == employee.DepartmentId.Value);

            var activePhoneAssignment = employee == null
                ? null
                : phoneAssignments
                    .Where(a => a.EmployeeId == employee.EmployeeId && a.IsActive)
                    .OrderByDescending(a => a.AssignedAtUtc)
                    .FirstOrDefault();

            var roleNames = userRoles
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => roles.FirstOrDefault(r => r.Id == ur.RoleId)?.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .OrderByDescending(GetRoleLevel)
                .ThenBy(name => name)
                .ToList();

            var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            return new UserAdminUserDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                EmployeeId = employee?.EmployeeId,
                DisplayName = employee?.Name,
                DepartmentId = employee?.DepartmentId,
                DepartmentName = department?.Name,
                ActivePhoneAssignmentId = activePhoneAssignment?.PhoneAssignmentId,
                ActiveWorkPhoneId = activePhoneAssignment?.WorkPhoneId,
                ActivePhoneDisplayName = activePhoneAssignment == null
                    ? null
                    : FormatPhoneDisplay(activePhoneAssignment.PhoneLabelSnapshot, activePhoneAssignment.PhoneNumberSnapshot),
                Roles = roleNames,
                IsActive = !isLockedOut,
                LockoutEnd = user.LockoutEnd
            };
        }

        private static string FormatPhoneDisplay(string? label, string number)
        {
            return string.IsNullOrWhiteSpace(label)
                ? number
                : $"{label} ({number})";
        }

        private static string NormalizeRequired(string value, int maxLength, string parameterName)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException($"{parameterName} mangler.", parameterName);
            }

            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }

        private static string NormalizeDisplayName(string? displayName, string fallbackEmail)
        {
            var normalized = displayName?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = fallbackEmail.Trim();
            }

            return normalized.Length > MaxDisplayNameLength ? normalized[..MaxDisplayNameLength] : normalized;
        }

        private static string NormalizeKey(string? value)
        {
            return value?.Trim().ToUpperInvariant() ?? string.Empty;
        }
    }
}
