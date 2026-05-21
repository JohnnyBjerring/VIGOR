using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Controllers.Api;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Controllers;

public class AuditEventsApiControllerTests
{
    [Fact]
    public async Task GetAuditEvents_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetAuditEvents(1, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetAuditEvents_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "missing-user");

        var result = await controller.GetAuditEvents(1, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetAuditEvents_ReturnsNotFound_WhenCitizenIsNotInUsersDepartment()
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

        var result = await controller.GetAuditEvents(citizen.CitizenId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetAuditEvents_ReturnsOkWithEvents_ForCitizenInUsersDepartment()
    {
        using var context = CreateContext();

        var (department, employee, citizen) = await CreateDepartmentEmployeeAndCitizenAsync(context);
        context.AuditEvents.Add(new AuditEvent
        {
            EntityType = "PnMedication",
            EntityId = 10,
            Action = "PnMedicationRegistered",
            Description = "PN-medicin 'Panodil' registreret.",
            UserId = employee.IdentityUserId,
            UserDisplayNameSnapshot = employee.Name,
            DepartmentId = department.DepartmentId,
            CitizenId = citizen.CitizenId,
            ShiftType = ShiftType.Evening,
            CreatedAtUtc = new DateTime(2026, 5, 21, 10, 30, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GetAuditEvents(citizen.CitizenId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IReadOnlyList<AuditEventDto>>(ok.Value);
        var auditEvent = Assert.Single(events);

        Assert.Equal("PnMedicationRegistered", auditEvent.Action);
        Assert.Equal("Employee", auditEvent.UserDisplayNameSnapshot);
        Assert.Equal(ShiftType.Evening, auditEvent.ShiftType);
        Assert.Equal("Aftenvagt", auditEvent.ShiftDisplayName);
    }

    private static AuditEventsApiController CreateController(AppDbContext context, string? userId = null, string role = "Personale")
    {
        var service = new AuditService(context);

        var controller = new AuditEventsApiController(
            service,
            context,
            NullLogger<AuditEventsApiController>.Instance);

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

    private static async Task<(Department Department, Employee Employee, Citizen Citizen)> CreateDepartmentEmployeeAndCitizenAsync(AppDbContext context)
    {
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var employee = new Employee
        {
            IdentityUserId = "user-1",
            Name = "Employee",
            DepartmentId = department.DepartmentId
        };

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Employees.Add(employee);
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        return (department, employee, citizen);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
