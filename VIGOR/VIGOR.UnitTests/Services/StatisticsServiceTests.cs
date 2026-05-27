using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.Constants;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class StatisticsServiceTests
{
    [Fact]
    public async Task GetDepartmentStatisticsAsync_CountsOnlySelectedDepartmentAndDateRange()
    {
        using var context = CreateContext();
        var departmentA = new Department { Name = "Afdeling A" };
        var departmentB = new Department { Name = "Afdeling B" };
        context.Departments.AddRange(departmentA, departmentB);
        await context.SaveChangesAsync();

        var from = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        AddAudit(context, departmentA.DepartmentId, AuditActions.CitizenStatusUpdated, from.AddDays(1));
        AddAudit(context, departmentA.DepartmentId, AuditActions.FixedMedicationGiven, from.AddDays(2));
        AddAudit(context, departmentA.DepartmentId, AuditActions.PnMedicationRegistered, from.AddDays(3));
        AddAudit(context, departmentA.DepartmentId, AuditActions.TaskCreated, from.AddDays(4));
        AddAudit(context, departmentA.DepartmentId, AuditActions.TaskCompleted, from.AddDays(5));
        AddAudit(context, departmentA.DepartmentId, AuditActions.TaskCreated, from.AddDays(-1));
        AddAudit(context, departmentB.DepartmentId, AuditActions.TaskCreated, from.AddDays(6));

        context.CitizenTasks.AddRange(
            new CitizenTask
            {
                CitizenId = 1,
                DepartmentId = departmentA.DepartmentId,
                ShiftType = ShiftType.Day,
                Title = "Åben opgave",
                Description = string.Empty,
                CreatedByUserId = "user-a",
                CreatedAtUtc = from.AddDays(4),
                IsCompleted = false
            },
            new CitizenTask
            {
                CitizenId = 2,
                DepartmentId = departmentA.DepartmentId,
                ShiftType = ShiftType.Day,
                Title = "Afsluttet opgave",
                Description = string.Empty,
                CreatedByUserId = "user-a",
                CreatedAtUtc = from.AddDays(4),
                IsCompleted = true,
                CompletedAtUtc = from.AddDays(5),
                CompletedByUserId = "user-b"
            },
            new CitizenTask
            {
                CitizenId = 3,
                DepartmentId = departmentB.DepartmentId,
                ShiftType = ShiftType.Day,
                Title = "Anden afdeling",
                Description = string.Empty,
                CreatedByUserId = "user-b",
                CreatedAtUtc = from.AddDays(4),
                IsCompleted = false
            });
        await context.SaveChangesAsync();

        var service = new StatisticsService(context);

        var result = await service.GetDepartmentStatisticsAsync(departmentA.DepartmentId, departmentA.Name, from, to);

        Assert.Equal("Afdeling A", result.ScopeDisplayName);
        Assert.False(result.IsSystemWide);
        Assert.Equal(1, result.StatusChangeCount);
        Assert.Equal(1, result.FixedMedicationRegistrationCount);
        Assert.Equal(1, result.PnMedicationRegistrationCount);
        Assert.Equal(1, result.TaskCreatedCount);
        Assert.Equal(1, result.TaskCompletedCount);
        Assert.Equal(1, result.OpenTaskCount);
    }

    [Fact]
    public async Task GetSystemStatisticsAsync_ReturnsAnonymousAggregateCountsWithoutSensitiveText()
    {
        using var context = CreateContext();
        var department = new Department { Name = "Hemmelig Afdeling" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        AddAudit(context, department.DepartmentId, AuditActions.CitizenStatusUpdated, new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc), "Jens Hansen blev ændret", "secret-user");
        AddAudit(context, department.DepartmentId, AuditActions.PnMedicationRegistered, new DateTime(2026, 5, 1, 11, 0, 0, DateTimeKind.Utc), "Panodil hemmelig medicin", "secret-user");
        await context.SaveChangesAsync();

        var service = new StatisticsService(context);

        var result = await service.GetSystemStatisticsAsync();
        var json = JsonSerializer.Serialize(result);

        Assert.True(result.IsSystemWide);
        Assert.Equal("Hele systemet", result.ScopeDisplayName);
        Assert.Equal(1, result.StatusChangeCount);
        Assert.Equal(1, result.PnMedicationRegistrationCount);
        Assert.DoesNotContain("Jens", json);
        Assert.DoesNotContain("Panodil", json);
        Assert.DoesNotContain("secret-user", json);
        Assert.DoesNotContain("Hemmelig Afdeling", json);
    }

    private static void AddAudit(
        AppDbContext context,
        int departmentId,
        string action,
        DateTime createdAtUtc,
        string description = "Hændelse",
        string userId = "user-1")
    {
        context.AuditEvents.Add(new AuditEvent
        {
            EntityType = "Test",
            EntityId = 1,
            Action = action,
            Description = description,
            UserId = userId,
            UserDisplayNameSnapshot = userId,
            DepartmentId = departmentId,
            CitizenId = 1,
            ShiftType = ShiftType.Day,
            CreatedAtUtc = createdAtUtc
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
