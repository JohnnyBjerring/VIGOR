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

public class ShiftsApiControllerTests
{
    [Fact]
    public async Task SelectShift_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.SelectShift(
            new SelectShiftRequest { ShiftType = ShiftType.Day },
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task SelectShift_ReturnsForbid_WhenEmployeeIsMissingOrHasNoDepartment()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "missing-user");

        var result = await controller.SelectShift(
            new SelectShiftRequest { ShiftType = ShiftType.Day },
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task SelectShift_ReturnsBadRequest_WhenShiftTypeIsInvalid()
    {
        using var context = CreateContext();
        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);
        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.SelectShift(
            new SelectShiftRequest { ShiftType = (ShiftType)999 },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Den valgte vagttype er ugyldig.", badRequest.Value);
        Assert.True(department.DepartmentId > 0);
    }

    [Theory]
    [InlineData(ShiftType.Day, "Dagvagt")]
    [InlineData(ShiftType.Evening, "Aftenvagt")]
    [InlineData(ShiftType.Night, "Nattevagt")]
    public async Task SelectShift_ReturnsOkWithActiveShiftContext_WhenInputIsValid(ShiftType shiftType, string expectedDisplayName)
    {
        using var context = CreateContext();
        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);
        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.SelectShift(
            new SelectShiftRequest { ShiftType = shiftType },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ActiveShiftContextDto>(ok.Value);

        Assert.Equal(shiftType, dto.ShiftType);
        Assert.Equal(expectedDisplayName, dto.DisplayName);
        Assert.Equal(employee.IdentityUserId, dto.SelectedByUserId);
        Assert.Equal(department.DepartmentId, dto.DepartmentId);
        Assert.True(dto.SelectedAtUtc <= DateTime.UtcNow);
    }

    private static ShiftsApiController CreateController(AppDbContext context, string? userId = null, string role = "Personale")
    {
        var controller = new ShiftsApiController(
            new ShiftSelectionService(),
            context,
            NullLogger<ShiftsApiController>.Instance);

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

    private static async Task<(Department Department, Employee Employee)> CreateDepartmentAndEmployeeAsync(
        AppDbContext context,
        string userId = "user-1")
    {
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var employee = new Employee
        {
            IdentityUserId = userId,
            Name = "Employee",
            DepartmentId = department.DepartmentId
        };

        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        return (department, employee);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
