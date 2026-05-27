using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VIGOR.Shared.Constants;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Controllers.Api;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Controllers;

public class StatisticsApiControllerTests
{
    [Fact]
    public async Task GetStatistics_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetStatistics(null, null, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetStatistics_ReturnsForbid_WhenUserIsNotLeaderOrSuperuser()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "staff-user", "Personale");

        var result = await controller.GetStatistics(null, null, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetStatistics_ReturnsBadRequest_WhenDateRangeIsInvalid()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "leader-user", "Leder");

        var result = await controller.GetStatistics(
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetStatistics_ReturnsDepartmentStatistics_ForLeader()
    {
        using var context = CreateContext();
        var department = new Department { Name = "Afdeling A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        context.Employees.Add(new Employee
        {
            IdentityUserId = "leader-user",
            Name = "Leder Jensen",
            DepartmentId = department.DepartmentId
        });
        AddAudit(context, department.DepartmentId, AuditActions.CitizenStatusUpdated);
        await context.SaveChangesAsync();

        var controller = CreateController(context, "leader-user", "Leder");

        var result = await controller.GetStatistics(null, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<StatisticsOverviewDto>(ok.Value);
        Assert.Equal("Afdeling A", dto.ScopeDisplayName);
        Assert.False(dto.IsSystemWide);
        Assert.Equal(1, dto.StatusChangeCount);
    }

    [Fact]
    public async Task GetStatistics_ReturnsSystemStatistics_ForPureSuperuser()
    {
        using var context = CreateContext();
        var department = new Department { Name = "Afdeling A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        AddAudit(context, department.DepartmentId, AuditActions.PnMedicationRegistered);
        await context.SaveChangesAsync();

        var controller = CreateController(context, "super-user", "Superbruger");

        var result = await controller.GetStatistics(null, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<StatisticsOverviewDto>(ok.Value);
        Assert.True(dto.IsSystemWide);
        Assert.Equal("Hele systemet", dto.ScopeDisplayName);
        Assert.Equal(1, dto.PnMedicationRegistrationCount);
    }

    [Fact]
    public async Task GetStatistics_ReturnsForbid_WhenLeaderHasNoDepartment()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "leader-user", "Leder");

        var result = await controller.GetStatistics(null, null, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    private static StatisticsApiController CreateController(
        AppDbContext context,
        string? userId = null,
        params string[] roles)
    {
        var service = new StatisticsService(context);
        var controller = new StatisticsApiController(
            service,
            context,
            NullLogger<StatisticsApiController>.Instance);

        var httpContext = new DefaultHttpContext();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private static void AddAudit(AppDbContext context, int departmentId, string action)
    {
        context.AuditEvents.Add(new AuditEvent
        {
            EntityType = "Test",
            EntityId = 1,
            Action = action,
            Description = "Hændelse",
            UserId = "user-1",
            UserDisplayNameSnapshot = "User",
            DepartmentId = departmentId,
            CitizenId = 1,
            ShiftType = ShiftType.Day,
            CreatedAtUtc = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc)
        });
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
