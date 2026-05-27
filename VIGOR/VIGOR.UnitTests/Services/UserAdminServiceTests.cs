using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class UserAdminServiceTests
{
    [Fact]
    public async Task GetUsersAsync_ReturnsUsersWithRolesAndDepartment()
    {
        using var context = CreateContext();
        var department = await AddDepartmentAsync(context, "Afdeling A");
        var role = await AddRoleAsync(context, "Leder");
        var user = await AddUserAsync(context, "leder@vigor.dk");
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
        context.Employees.Add(new Employee { IdentityUserId = user.Id, Name = "Leder Jensen", DepartmentId = department.DepartmentId });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetUsersAsync();

        var dto = Assert.Single(result);
        Assert.Equal(user.Id, dto.UserId);
        Assert.Equal("leder@vigor.dk", dto.Email);
        Assert.Equal("Leder Jensen", dto.DisplayName);
        Assert.Equal(department.DepartmentId, dto.DepartmentId);
        Assert.Equal("Afdeling A", dto.DepartmentName);
        Assert.Equal("Leder", dto.PrimaryRole);
        Assert.True(dto.IsActive);
    }


    [Fact]
    public async Task GetRolesAsync_ReturnsRolesOrderedByRankDescending()
    {
        using var context = CreateContext();
        await AddRoleAsync(context, "Personale");
        await AddRoleAsync(context, "Leder");
        await AddRoleAsync(context, "Vagtansvarlig");
        await AddRoleAsync(context, "Superbruger");

        var service = CreateService(context);

        var result = await service.GetRolesAsync();

        Assert.Equal(["Superbruger", "Leder", "Vagtansvarlig", "Personale"], result.Select(r => r.Name));
    }

    [Fact]
    public async Task CreateUserAsync_CreatesIdentityUserEmployeeAndRole()
    {
        using var context = CreateContext();
        var department = await AddDepartmentAsync(context, "Afdeling B");
        await AddRoleAsync(context, "Personale");

        var service = CreateService(context);

        var result = await service.CreateUserAsync(new CreateUserAdminUserRequest
        {
            Email = "ny@vigor.dk",
            Password = "Test1234",
            DisplayName = "Ny Medarbejder",
            DepartmentId = department.DepartmentId,
            RoleName = "Personale"
        }, ["Leder"]);

        Assert.Equal("ny@vigor.dk", result.Email);
        Assert.Equal("Ny Medarbejder", result.DisplayName);
        Assert.Equal(department.DepartmentId, result.DepartmentId);
        Assert.Equal("Personale", result.PrimaryRole);
        Assert.True(result.IsActive);

        var dbUser = await context.Users.SingleAsync(u => u.Email == "ny@vigor.dk");
        Assert.False(string.IsNullOrWhiteSpace(dbUser.PasswordHash));
        Assert.True(await context.UserRoles.AnyAsync(ur => ur.UserId == dbUser.Id));
        Assert.True(await context.Employees.AnyAsync(e => e.IdentityUserId == dbUser.Id && e.DepartmentId == department.DepartmentId));
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsInvalidOperation_WhenEmailAlreadyExists()
    {
        using var context = CreateContext();
        var department = await AddDepartmentAsync(context, "Afdeling A");
        await AddRoleAsync(context, "Personale");
        await AddUserAsync(context, "personale@vigor.dk");

        var service = CreateService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUserAsync(new CreateUserAdminUserRequest
        {
            Email = "personale@vigor.dk",
            Password = "Test1234",
            DisplayName = "Dublet",
            DepartmentId = department.DepartmentId,
            RoleName = "Personale"
        }, ["Leder"]));
    }

    [Fact]
    public async Task UpdateRoleAsync_ReplacesExistingRole()
    {
        using var context = CreateContext();
        await AddDepartmentAsync(context, "Afdeling A");
        var staffRole = await AddRoleAsync(context, "Personale");
        var leaderRole = await AddRoleAsync(context, "Leder");
        var user = await AddUserAsync(context, "bruger@vigor.dk");
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = staffRole.Id });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.UpdateRoleAsync(user.Id, new UpdateUserRoleRequest { RoleName = "Leder" }, "admin-user", ["Leder"]);

        Assert.NotNull(result);
        Assert.Equal("Leder", result!.PrimaryRole);
        var userRoles = await context.UserRoles.Where(ur => ur.UserId == user.Id).ToListAsync();
        var role = Assert.Single(userRoles);
        Assert.Equal(leaderRole.Id, role.RoleId);
    }

    [Fact]
    public async Task UpdateRoleAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        using var context = CreateContext();
        await AddRoleAsync(context, "Leder");
        var service = CreateService(context);

        var result = await service.UpdateRoleAsync("missing", new UpdateUserRoleRequest { RoleName = "Leder" }, "admin-user", ["Leder"]);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetActiveAsync_DeactivatesAndReactivatesUser()
    {
        using var context = CreateContext();
        var user = await AddUserAsync(context, "aktiv@vigor.dk");
        var service = CreateService(context);

        var deactivated = await service.SetActiveAsync(user.Id, new SetUserActiveRequest { IsActive = false }, "admin-user", ["Leder"]);

        Assert.NotNull(deactivated);
        Assert.False(deactivated!.IsActive);
        var dbUser = await context.Users.SingleAsync(u => u.Id == user.Id);
        Assert.True(dbUser.LockoutEnd > DateTimeOffset.UtcNow);
        Assert.Equal(DateTimeOffset.MaxValue, dbUser.LockoutEnd);
        Assert.Equal(0, dbUser.AccessFailedCount);

        var activated = await service.SetActiveAsync(user.Id, new SetUserActiveRequest { IsActive = true }, "admin-user", ["Leder"]);

        Assert.NotNull(activated);
        Assert.True(activated!.IsActive);
        var reactivatedUser = await context.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Null(reactivatedUser.LockoutEnd);
        Assert.Equal(0, reactivatedUser.AccessFailedCount);
    }

    [Fact]
    public async Task SetActiveAsync_ThrowsInvalidOperation_WhenUserTriesToDeactivateSelf()
    {
        using var context = CreateContext();
        var user = await AddUserAsync(context, "admin@vigor.dk");
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetActiveAsync(user.Id, new SetUserActiveRequest { IsActive = false }, user.Id, ["Leder"]));

        Assert.Equal("Du kan ikke deaktivere den bruger, du selv er logget ind med.", ex.Message);

        var dbUser = await context.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Null(dbUser.LockoutEnd);
    }


    [Fact]
    public async Task UpdateRoleAsync_ThrowsInvalidOperation_WhenUserTriesToChangeOwnRole()
    {
        using var context = CreateContext();
        var staffRole = await AddRoleAsync(context, "Personale");
        await AddRoleAsync(context, "Leder");
        var user = await AddUserAsync(context, "leder@vigor.dk");
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = staffRole.Id });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateRoleAsync(user.Id, new UpdateUserRoleRequest { RoleName = "Leder" }, user.Id, ["Leder"]));

        Assert.Equal("Du kan ikke ændre rollen på den bruger, du selv er logget ind med.", ex.Message);
    }

    [Fact]
    public async Task UpdateRoleAsync_ThrowsInvalidOperation_WhenActorAssignsHigherRoleThanOwn()
    {
        using var context = CreateContext();
        var staffRole = await AddRoleAsync(context, "Personale");
        await AddRoleAsync(context, "Superbruger");
        var user = await AddUserAsync(context, "personale@vigor.dk");
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = staffRole.Id });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateRoleAsync(user.Id, new UpdateUserRoleRequest { RoleName = "Superbruger" }, "leader-user", ["Leder"]));

        Assert.Equal("Du kan ikke tildele en rolle, der er højere end din egen rolle.", ex.Message);
    }

    [Fact]
    public async Task UpdateRoleAsync_ThrowsInvalidOperation_WhenTargetHasHigherRoleThanActor()
    {
        using var context = CreateContext();
        var superUserRole = await AddRoleAsync(context, "Superbruger");
        await AddRoleAsync(context, "Personale");
        var target = await AddUserAsync(context, "super@vigor.dk");
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = target.Id, RoleId = superUserRole.Id });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateRoleAsync(target.Id, new UpdateUserRoleRequest { RoleName = "Personale" }, "leader-user", ["Leder"]));

        Assert.Equal("Du kan ikke ændre en bruger med højere rolle end din egen.", ex.Message);
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsInvalidOperation_WhenActorCreatesHigherRoleThanOwn()
    {
        using var context = CreateContext();
        var department = await AddDepartmentAsync(context, "Afdeling A");
        await AddRoleAsync(context, "Superbruger");

        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUserAsync(new CreateUserAdminUserRequest
        {
            Email = "super@vigor.dk",
            Password = "Test1234",
            DisplayName = "Superbruger",
            DepartmentId = department.DepartmentId,
            RoleName = "Superbruger"
        }, ["Leder"]));

        Assert.Equal("Du kan ikke tildele en rolle, der er højere end din egen rolle.", ex.Message);
    }

    [Fact]
    public async Task UpdateRoleAsync_AllowsSuperuserToAssignLeaderRole()
    {
        using var context = CreateContext();
        var staffRole = await AddRoleAsync(context, "Personale");
        var leaderRole = await AddRoleAsync(context, "Leder");
        var user = await AddUserAsync(context, "bruger@vigor.dk");
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = staffRole.Id });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.UpdateRoleAsync(user.Id, new UpdateUserRoleRequest { RoleName = "Leder" }, "super-user", ["Superbruger"]);

        Assert.NotNull(result);
        Assert.Equal("Leder", result!.PrimaryRole);
        var userRole = await context.UserRoles.SingleAsync(ur => ur.UserId == user.Id);
        Assert.Equal(leaderRole.Id, userRole.RoleId);
    }

    private static UserAdminService CreateService(AppDbContext context)
    {
        return new UserAdminService(context, new PasswordHasher<IdentityUser>());
    }

    private static async Task<Department> AddDepartmentAsync(AppDbContext context, string name)
    {
        var department = new Department { Name = name };
        context.Departments.Add(department);
        await context.SaveChangesAsync();
        return department;
    }

    private static async Task<IdentityRole> AddRoleAsync(AppDbContext context, string name)
    {
        var role = new IdentityRole
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant()
        };
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        return role;
    }

    private static async Task<IdentityUser> AddUserAsync(AppDbContext context, string email)
    {
        var user = new IdentityUser
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            LockoutEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString("D"),
            ConcurrencyStamp = Guid.NewGuid().ToString("D")
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
