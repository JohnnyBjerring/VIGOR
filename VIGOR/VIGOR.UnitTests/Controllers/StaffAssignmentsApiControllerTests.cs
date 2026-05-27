using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Models;
using VIGOR.Web.Controllers.Api;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Controllers;

public class StaffAssignmentsApiControllerTests
{
    [Fact]
    public async Task GetAssignments_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetAssignments(1, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetAssignments_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "invalid-user");

        var result = await controller.GetAssignments(1, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetAssignments_ReturnsNotFound_WhenCitizenNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = depB.DepartmentId };
        var employee = new Employee
        {
            IdentityUserId = "user-1",
            Name = "Employee",
            DepartmentId = depA.DepartmentId
        };

        context.Citizens.Add(citizen);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GetAssignments(citizen.CitizenId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetAssignments_ReturnsOk_ForCitizenInDepartment()
    {
        using var context = CreateContext();

        var (department, actor, citizen, staff) = await CreateDepartmentActorCitizenAndStaffAsync(context);
        context.CitizenStaffAssignments.Add(new CitizenStaffAssignment
        {
            CitizenId = citizen.CitizenId,
            DepartmentId = department.DepartmentId,
            EmployeeId = staff.EmployeeId,
            EmployeeNameSnapshot = staff.Name,
            AssignedByUserId = actor.IdentityUserId,
            AssignedAtUtc = DateTime.UtcNow,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, actor.IdentityUserId);

        var result = await controller.GetAssignments(citizen.CitizenId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var assignments = Assert.IsAssignableFrom<IReadOnlyList<CitizenStaffAssignmentDto>>(ok.Value);
        Assert.Single(assignments);
    }

    [Fact]
    public async Task GetAssignableStaff_ReturnsOk_ForCitizenInDepartment()
    {
        using var context = CreateContext();

        var (_, actor, citizen, staff) = await CreateDepartmentActorCitizenAndStaffAsync(context);
        var controller = CreateController(context, actor.IdentityUserId);

        var result = await controller.GetAssignableStaff(citizen.CitizenId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var staffList = Assert.IsAssignableFrom<IReadOnlyList<AssignableStaffDto>>(ok.Value);
        Assert.Contains(staffList, s => s.EmployeeId == staff.EmployeeId);
    }

    [Fact]
    public async Task AssignStaff_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.AssignStaff(1, new AssignStaffToCitizenRequest { EmployeeId = 1 }, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task AssignStaff_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "invalid-user", "Vagtansvarlig");

        var result = await controller.AssignStaff(1, new AssignStaffToCitizenRequest { EmployeeId = 1 }, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AssignStaff_ReturnsBadRequest_WhenRequestInvalid()
    {
        using var context = CreateContext();

        var (_, actor, citizen, _) = await CreateDepartmentActorCitizenAndStaffAsync(context);
        var controller = CreateController(context, actor.IdentityUserId, "Vagtansvarlig");

        var result = await controller.AssignStaff(citizen.CitizenId, new AssignStaffToCitizenRequest { EmployeeId = 0 }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AssignStaff_ReturnsNotFound_WhenStaffNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var actor = new Employee { IdentityUserId = "actor-user", Name = "Actor", DepartmentId = depA.DepartmentId };
        var citizen = new Citizen { Name = "Citizen", DepartmentId = depA.DepartmentId };
        var staff = new Employee { IdentityUserId = "staff-user", Name = "Staff", DepartmentId = depB.DepartmentId };
        context.Employees.AddRange(actor, staff);
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var controller = CreateController(context, actor.IdentityUserId, "Vagtansvarlig");

        var result = await controller.AssignStaff(citizen.CitizenId, new AssignStaffToCitizenRequest { EmployeeId = staff.EmployeeId }, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AssignStaff_ReturnsCreated_WhenRequestIsValid()
    {
        using var context = CreateContext();

        var (department, actor, citizen, staff) = await CreateDepartmentActorCitizenAndStaffAsync(context);
        var controller = CreateController(context, actor.IdentityUserId, "Vagtansvarlig");

        var result = await controller.AssignStaff(citizen.CitizenId, new AssignStaffToCitizenRequest { EmployeeId = staff.EmployeeId }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<CitizenStaffAssignmentDto>(created.Value);

        Assert.Equal(citizen.CitizenId, dto.CitizenId);
        Assert.Equal(department.DepartmentId, dto.DepartmentId);
        Assert.Equal(staff.EmployeeId, dto.EmployeeId);
        Assert.Equal(staff.Name, dto.EmployeeNameSnapshot);
        Assert.True(dto.IsActive);

        var dbAssignment = await context.CitizenStaffAssignments.SingleAsync();
        Assert.Equal(staff.EmployeeId, dbAssignment.EmployeeId);
    }

    [Fact]
    public async Task UnassignStaff_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.UnassignStaff(1, 1, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UnassignStaff_ReturnsNotFound_WhenAssignmentNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var actor = new Employee { IdentityUserId = "actor-user", Name = "Actor", DepartmentId = depA.DepartmentId };
        var citizen = new Citizen { Name = "Citizen", DepartmentId = depB.DepartmentId };
        context.Employees.Add(actor);
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var controller = CreateController(context, actor.IdentityUserId, "Vagtansvarlig");

        var result = await controller.UnassignStaff(citizen.CitizenId, 123, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UnassignStaff_ReturnsOk_WhenAssignmentExists()
    {
        using var context = CreateContext();

        var (department, actor, citizen, staff) = await CreateDepartmentActorCitizenAndStaffAsync(context);
        var assignment = new CitizenStaffAssignment
        {
            CitizenId = citizen.CitizenId,
            DepartmentId = department.DepartmentId,
            EmployeeId = staff.EmployeeId,
            EmployeeNameSnapshot = staff.Name,
            AssignedByUserId = actor.IdentityUserId,
            AssignedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        context.CitizenStaffAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var controller = CreateController(context, actor.IdentityUserId, "Vagtansvarlig");

        var result = await controller.UnassignStaff(citizen.CitizenId, assignment.CitizenStaffAssignmentId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<CitizenStaffAssignmentDto>(ok.Value);
        Assert.False(dto.IsActive);
        Assert.Equal(actor.IdentityUserId, dto.UnassignedByUserId);

        var dbAssignment = await context.CitizenStaffAssignments.SingleAsync();
        Assert.False(dbAssignment.IsActive);
    }

    private static StaffAssignmentsApiController CreateController(AppDbContext context, string? userId = null, string role = "Personale")
    {
        var service = new StaffAssignmentService(context);

        var controller = new StaffAssignmentsApiController(
            service,
            context,
            NullLogger<StaffAssignmentsApiController>.Instance);

        var httpContext = new DefaultHttpContext();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            }, "jwt"));
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private static async Task<(Department Department, Employee Actor, Citizen Citizen, Employee Staff)> CreateDepartmentActorCitizenAndStaffAsync(AppDbContext context)
    {
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var actor = new Employee
        {
            IdentityUserId = "actor-user",
            Name = "Vagtansvarlig",
            DepartmentId = department.DepartmentId
        };

        var staff = new Employee
        {
            IdentityUserId = "staff-user",
            Name = "Medarbejder Jensen",
            DepartmentId = department.DepartmentId
        };

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Employees.AddRange(actor, staff);
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        return (department, actor, citizen, staff);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
