using VIGOR.Shared.DTOs;

namespace VIGOR.Web.Services
{
    /// <summary>
    /// Server-intern UC13 service til smal bruger- og rolleadministration.
    /// </summary>
    public interface IUserAdminService
    {
        Task<IReadOnlyList<UserAdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserAdminRoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserAdminDepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);

        Task<UserAdminUserDto> CreateUserAsync(
            CreateUserAdminUserRequest request,
            IReadOnlyCollection<string> actorRoleNames,
            CancellationToken cancellationToken = default);

        Task<UserAdminUserDto?> UpdateRoleAsync(
            string userId,
            UpdateUserRoleRequest request,
            string actorUserId,
            IReadOnlyCollection<string> actorRoleNames,
            CancellationToken cancellationToken = default);

        Task<UserAdminUserDto?> SetActiveAsync(
            string userId,
            SetUserActiveRequest request,
            string actorUserId,
            IReadOnlyCollection<string> actorRoleNames,
            CancellationToken cancellationToken = default);
    }
}
