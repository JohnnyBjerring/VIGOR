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

public class PnMedicationsApiControllerTests
{
    [Fact]
    public async Task GetPnMedications_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetPnMedications(1, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetPnMedications_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "invalid-user");

        var result = await controller.GetPnMedications(1, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetPnMedications_ReturnsNotFound_WhenCitizenNotInDepartment()
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

        var result = await controller.GetPnMedications(citizen.CitizenId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetPnMedications_ReturnsOk_ForCitizenInDepartment()
    {
        using var context = CreateContext();

        var (department, employee, citizen) = await CreateDepartmentEmployeeAndCitizenAsync(context);
        context.PnMedications.Add(new PnMedication
        {
            CitizenId = citizen.CitizenId,
            DepartmentId = department.DepartmentId,
            ShiftType = ShiftType.Day,
            MedicineName = "Panodil",
            Dose = "1 tablet",
            Reason = "Smerter",
            GivenAtUtc = new DateTime(2026, 5, 18, 8, 0, 0, DateTimeKind.Utc),
            GivenByUserId = employee.IdentityUserId,
            CreatedAtUtc = new DateTime(2026, 5, 18, 8, 1, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GetPnMedications(citizen.CitizenId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var registrations = Assert.IsAssignableFrom<IReadOnlyList<PnMedicationDto>>(ok.Value);
        Assert.Single(registrations);
    }

    [Fact]
    public async Task RegisterPnMedication_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.RegisterPnMedication(1, CreateValidRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task RegisterPnMedication_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "invalid-user");

        var result = await controller.RegisterPnMedication(1, CreateValidRequest(), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task RegisterPnMedication_ReturnsNotFound_WhenCitizenNotInDepartment()
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

        var result = await controller.RegisterPnMedication(citizen.CitizenId, CreateValidRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RegisterPnMedication_ReturnsBadRequest_WhenRequestInvalid()
    {
        using var context = CreateContext();

        var (_, employee, citizen) = await CreateDepartmentEmployeeAndCitizenAsync(context);
        var controller = CreateController(context, employee.IdentityUserId);
        var request = CreateValidRequest();
        request.MedicineName = string.Empty;

        var result = await controller.RegisterPnMedication(citizen.CitizenId, request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RegisterPnMedication_ReturnsCreated_WhenRequestIsValid()
    {
        using var context = CreateContext();

        var (department, employee, citizen) = await CreateDepartmentEmployeeAndCitizenAsync(context);
        var controller = CreateController(context, employee.IdentityUserId);
        var givenAt = new DateTime(2026, 5, 18, 10, 30, 0, DateTimeKind.Utc);
        var request = new RegisterPnMedicationRequest
        {
            MedicineName = "Panodil",
            Dose = "1 tablet",
            Reason = "Smerter",
            GivenAt = givenAt,
            ShiftType = ShiftType.Day
        };

        var result = await controller.RegisterPnMedication(citizen.CitizenId, request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<PnMedicationDto>(created.Value);

        Assert.Equal(citizen.CitizenId, dto.CitizenId);
        Assert.Equal(department.DepartmentId, dto.DepartmentId);
        Assert.Equal(ShiftType.Day, dto.ShiftType);
        Assert.Equal("Panodil", dto.MedicineName);
        Assert.Equal("1 tablet", dto.Dose);
        Assert.Equal("Smerter", dto.Reason);
        Assert.Equal(givenAt, dto.GivenAtUtc);
        Assert.Equal(employee.IdentityUserId, dto.GivenByUserId);

        var dbRegistration = await context.PnMedications.SingleAsync();
        Assert.Equal("Panodil", dbRegistration.MedicineName);
        Assert.Equal(employee.IdentityUserId, dbRegistration.GivenByUserId);
    }

    private static RegisterPnMedicationRequest CreateValidRequest()
    {
        return new RegisterPnMedicationRequest
        {
            MedicineName = "Panodil",
            Dose = "1 tablet",
            Reason = "Smerter",
            ShiftType = ShiftType.Day
        };
    }

    private static PnMedicationsApiController CreateController(AppDbContext context, string? userId = null, string role = "Personale")
    {
        var service = new PnMedicationService(context);

        var controller = new PnMedicationsApiController(
            service,
            context,
            NullLogger<PnMedicationsApiController>.Instance);

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
