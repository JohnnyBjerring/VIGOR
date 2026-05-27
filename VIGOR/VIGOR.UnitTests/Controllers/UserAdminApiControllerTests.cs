using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using VIGOR.Shared.DTOs;
using VIGOR.Web.Controllers.Api;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Controllers;

public class UserAdminApiControllerTests
{
    [Fact]
    public async Task GetUsers_ReturnsUnauthorized_WhenNoUser()
    {
        var controller = CreateController();

        var result = await controller.GetUsers(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetUsers_ReturnsForbid_WhenUserIsNotLeaderOrSuperuser()
    {
        var controller = CreateController("user-1", "Personale");

        var result = await controller.GetUsers(CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetUsers_ReturnsOk_WhenUserIsLeader()
    {
        var service = new FakeUserAdminService();
        service.Users.Add(new UserAdminUserDto { UserId = "u1", Email = "admin@vigor.dk", Roles = ["Leder"], IsActive = true });
        var controller = CreateController("leader-user", "Leder", service);

        var result = await controller.GetUsers(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsAssignableFrom<IReadOnlyList<UserAdminUserDto>>(ok.Value);
        Assert.Single(users);
    }

    [Fact]
    public async Task GetRoles_ReturnsOk_WhenUserIsSuperuser()
    {
        var service = new FakeUserAdminService();
        service.Roles.Add(new UserAdminRoleDto { Name = "Superbruger" });
        var controller = CreateController("super-user", "Superbruger", service);

        var result = await controller.GetRoles(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var roles = Assert.IsAssignableFrom<IReadOnlyList<UserAdminRoleDto>>(ok.Value);
        Assert.Single(roles);
    }

    [Fact]
    public async Task CreateUser_ReturnsCreated_WhenRequestIsValid()
    {
        var service = new FakeUserAdminService();
        var controller = CreateController("leader-user", "Leder", service);

        var result = await controller.CreateUser(new CreateUserAdminUserRequest
        {
            Email = "ny@vigor.dk",
            Password = "Test1234",
            DisplayName = "Ny Bruger",
            DepartmentId = 1,
            RoleName = "Personale"
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<UserAdminUserDto>(created.Value);
        Assert.Equal("ny@vigor.dk", dto.Email);
        Assert.Equal("Personale", dto.PrimaryRole);
    }

    [Fact]
    public async Task CreateUser_ReturnsConflict_WhenEmailAlreadyExists()
    {
        var service = new FakeUserAdminService { ThrowConflictOnCreate = true };
        var controller = CreateController("leader-user", "Leder", service);

        var result = await controller.CreateUser(new CreateUserAdminUserRequest
        {
            Email = "dublet@vigor.dk",
            Password = "Test1234",
            DisplayName = "Dublet",
            DepartmentId = 1,
            RoleName = "Personale"
        }, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task UpdateRole_ReturnsOk_WhenUserExists()
    {
        var service = new FakeUserAdminService();
        var controller = CreateController("leader-user", "Leder", service);

        var result = await controller.UpdateRole("u1", new UpdateUserRoleRequest { RoleName = "Vagtansvarlig" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<UserAdminUserDto>(ok.Value);
        Assert.Equal("Vagtansvarlig", dto.PrimaryRole);
    }


    [Fact]
    public async Task UpdateRole_ReturnsConflict_WhenServiceRejectsRoleChange()
    {
        var service = new FakeUserAdminService { ThrowConflictOnRoleUpdate = true };
        var controller = CreateController("leader-user", "Leder", service);

        var result = await controller.UpdateRole("leader-user", new UpdateUserRoleRequest { RoleName = "Superbruger" }, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Du kan ikke ændre rollen på den bruger, du selv er logget ind med.", conflict.Value);
    }

    [Fact]
    public async Task SetActive_ReturnsOk_WhenUserExists()
    {
        var service = new FakeUserAdminService();
        var controller = CreateController("leader-user", "Leder", service);

        var result = await controller.SetActive("u1", new SetUserActiveRequest { IsActive = false }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<UserAdminUserDto>(ok.Value);
        Assert.False(dto.IsActive);
    }

    [Fact]
    public async Task SetActive_ReturnsConflict_WhenUserTriesToDeactivateSelf()
    {
        var service = new FakeUserAdminService();
        var controller = CreateController("leader-user", "Leder", service);

        var result = await controller.SetActive("leader-user", new SetUserActiveRequest { IsActive = false }, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Du kan ikke deaktivere den bruger, du selv er logget ind med.", conflict.Value);
    }


    private static UserAdminApiController CreateController(
        string? userId = null,
        string role = "Leder",
        IUserAdminService? service = null)
    {
        var controller = new UserAdminApiController(service ?? new FakeUserAdminService(), NullLogger<UserAdminApiController>.Instance);
        var httpContext = new DefaultHttpContext();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            ], "TestAuth"));
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private sealed class FakeUserAdminService : IUserAdminService
    {
        public List<UserAdminUserDto> Users { get; } = new();
        public List<UserAdminRoleDto> Roles { get; } = new();
        public List<UserAdminDepartmentDto> Departments { get; } = new();
        public bool ThrowConflictOnCreate { get; set; }
        public bool ThrowConflictOnRoleUpdate { get; set; }

        public Task<IReadOnlyList<UserAdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<UserAdminUserDto>>(Users);
        }

        public Task<IReadOnlyList<UserAdminRoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<UserAdminRoleDto>>(Roles);
        }

        public Task<IReadOnlyList<UserAdminDepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<UserAdminDepartmentDto>>(Departments);
        }

        public Task<UserAdminUserDto> CreateUserAsync(
            CreateUserAdminUserRequest request,
            IReadOnlyCollection<string> actorRoleNames,
            CancellationToken cancellationToken = default)
        {
            if (ThrowConflictOnCreate)
            {
                throw new InvalidOperationException("Der findes allerede en bruger med den email.");
            }

            return Task.FromResult(new UserAdminUserDto
            {
                UserId = "new-user",
                Email = request.Email,
                DisplayName = request.DisplayName,
                DepartmentId = request.DepartmentId,
                Roles = [request.RoleName],
                IsActive = true
            });
        }

        public Task<UserAdminUserDto?> UpdateRoleAsync(
            string userId,
            UpdateUserRoleRequest request,
            string actorUserId,
            IReadOnlyCollection<string> actorRoleNames,
            CancellationToken cancellationToken = default)
        {
            if (ThrowConflictOnRoleUpdate)
            {
                throw new InvalidOperationException("Du kan ikke ændre rollen på den bruger, du selv er logget ind med.");
            }

            return Task.FromResult<UserAdminUserDto?>(new UserAdminUserDto
            {
                UserId = userId,
                Email = "bruger@vigor.dk",
                Roles = [request.RoleName],
                IsActive = true
            });
        }

        public Task<UserAdminUserDto?> SetActiveAsync(
            string userId,
            SetUserActiveRequest request,
            string actorUserId,
            IReadOnlyCollection<string> actorRoleNames,
            CancellationToken cancellationToken = default)
        {
            if (!request.IsActive && userId == actorUserId)
            {
                throw new InvalidOperationException("Du kan ikke deaktivere den bruger, du selv er logget ind med.");
            }

            return Task.FromResult<UserAdminUserDto?>(new UserAdminUserDto
            {
                UserId = userId,
                Email = "bruger@vigor.dk",
                Roles = ["Personale"],
                IsActive = request.IsActive
            });
        }
    }
}
