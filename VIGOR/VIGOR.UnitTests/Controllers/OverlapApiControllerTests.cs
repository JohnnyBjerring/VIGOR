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

public class OverlapApiControllerTests
{
    [Fact]
    public async Task GetOverlap_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetOverlap(ShiftType.Day, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetOverlap_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "missing-user");

        var result = await controller.GetOverlap(ShiftType.Day, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetOverlap_ReturnsBadRequest_WhenShiftTypeInvalid()
    {
        using var context = CreateContext();
        var (_, employee) = await CreateDepartmentAndEmployeeAsync(context);
        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GetOverlap((ShiftType)999, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetOverlap_ReturnsOk_ForEmployeeDepartment()
    {
        using var context = CreateContext();
        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);
        context.Citizens.Add(new Citizen
        {
            Name = "Anna",
            DepartmentId = department.DepartmentId,
            Status = CitizenStatus.Green
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GetOverlap(ShiftType.Night, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<OverlapDto>(ok.Value);
        Assert.Equal(department.DepartmentId, dto.DepartmentId);
        Assert.Equal(ShiftType.Night, dto.ActiveShiftType);
        Assert.Single(dto.Citizens);
    }

    private static OverlapApiController CreateController(AppDbContext context, string? userId = null, string role = "Personale")
    {
        var service = new OverlapService(context);

        var controller = new OverlapApiController(
            service,
            context,
            NullLogger<OverlapApiController>.Instance);

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

    private static async Task<(Department Department, Employee Employee)> CreateDepartmentAndEmployeeAsync(AppDbContext context)
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
