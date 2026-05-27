using VIGOR.Shared.DTOs;

namespace VIGOR.Shared.Interfaces.Services
{
    /// <summary>
    /// Client-side API-kontrakt for UC13 bruger- og rolleadministration.
    /// </summary>
    public interface IUserAdminApi
    {
        Task<IReadOnlyList<UserAdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserAdminRoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserAdminDepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);

        Task<UserAdminUserDto?> CreateUserAsync(
            CreateUserAdminUserRequest request,
            CancellationToken cancellationToken = default);

        Task<UserAdminUserDto?> UpdateRoleAsync(
            string userId,
            UpdateUserRoleRequest request,
            CancellationToken cancellationToken = default);

        Task<UserAdminUserDto?> SetActiveAsync(
            string userId,
            SetUserActiveRequest request,
            CancellationToken cancellationToken = default);
    }
}
