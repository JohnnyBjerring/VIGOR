using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class CitizenTaskServiceTests
{
    [Fact]
    public async Task GetForCitizenAsync_ReturnsNull_WhenCitizenNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = depA.DepartmentId };
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var service = new CitizenTaskService(context);

        var result = await service.GetForCitizenAsync(citizen.CitizenId, depB.DepartmentId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetForCitizenAsync_ReturnsActiveTasksBeforeCompletedTasks()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);

        context.CitizenTasks.AddRange(
            new CitizenTask
            {
                CitizenId = citizen.CitizenId,
                DepartmentId = department.DepartmentId,
                ShiftType = ShiftType.Day,
                Title = "Afsluttet opgave",
                Description = "Beskrivelse",
                CreatedByUserId = "user-1",
                CreatedAtUtc = new DateTime(2026, 5, 25, 8, 0, 0, DateTimeKind.Utc),
                IsCompleted = true,
                CompletedAtUtc = new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc),
                CompletedByUserId = "user-1"
            },
            new CitizenTask
            {
                CitizenId = citizen.CitizenId,
                DepartmentId = department.DepartmentId,
                ShiftType = ShiftType.Evening,
                Title = "Aktiv opgave",
                Description = "Beskrivelse",
                CreatedByUserId = "user-1",
                CreatedAtUtc = new DateTime(2026, 5, 25, 20, 0, 0, DateTimeKind.Utc),
                IsCompleted = false
            });

        await context.SaveChangesAsync();

        var service = new CitizenTaskService(context);

        var result = await service.GetForCitizenAsync(citizen.CitizenId, department.DepartmentId);

        Assert.NotNull(result);
        Assert.Collection(
            result!,
            first => Assert.Equal("Aktiv opgave", first.Title),
            second => Assert.Equal("Afsluttet opgave", second.Title));
    }

    [Fact]
    public async Task CreateAsync_CreatesTask_WhenRequestIsValid()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var service = new CitizenTaskService(context);

        var request = new CreateCitizenTaskRequest
        {
            Title = "Husk væskeskema",
            Description = "Følg op efter aftensmad.",
            ShiftType = ShiftType.Evening
        };

        var result = await service.CreateAsync(
            citizen.CitizenId,
            department.DepartmentId,
            "user-1",
            request);

        Assert.NotNull(result);
        Assert.Equal(citizen.CitizenId, result!.CitizenId);
        Assert.Equal(department.DepartmentId, result.DepartmentId);
        Assert.Equal(ShiftType.Evening, result.ShiftType);
        Assert.Equal("Aftenvagt", result.ShiftDisplayName);
        Assert.Equal("Husk væskeskema", result.Title);
        Assert.Equal("Følg op efter aftensmad.", result.Description);
        Assert.Equal("user-1", result.CreatedByUserId);
        Assert.False(result.IsCompleted);

        var dbTask = await context.CitizenTasks.SingleAsync();
        Assert.Equal("Husk væskeskema", dbTask.Title);
        Assert.Equal("user-1", dbTask.CreatedByUserId);
        Assert.False(dbTask.IsCompleted);
    }

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenCitizenNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = depA.DepartmentId };
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var service = new CitizenTaskService(context);
        var request = CreateValidRequest();

        var result = await service.CreateAsync(citizen.CitizenId, depB.DepartmentId, "user-1", request);

        Assert.Null(result);
        Assert.Empty(context.CitizenTasks);
    }

    [Fact]
    public async Task CreateAsync_ThrowsArgumentException_WhenTitleMissing()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var service = new CitizenTaskService(context);
        var request = CreateValidRequest();
        request.Title = "   ";

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(citizen.CitizenId, department.DepartmentId, "user-1", request));
    }

    [Fact]
    public async Task CreateAsync_ThrowsArgumentException_WhenShiftTypeInvalid()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var service = new CitizenTaskService(context);
        var request = CreateValidRequest();
        request.ShiftType = (ShiftType)999;

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(citizen.CitizenId, department.DepartmentId, "user-1", request));
    }

    [Fact]
    public async Task CreateAsync_WritesAuditEvent_WhenAuditServiceIsInjected()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var auditService = new AuditService(context);
        var service = new CitizenTaskService(context, auditService);

        var result = await service.CreateAsync(
            citizen.CitizenId,
            department.DepartmentId,
            "user-2",
            CreateValidRequest(),
            userDisplayNameSnapshot: "Bent Jensen");

        Assert.NotNull(result);

        var auditEvent = await context.AuditEvents.SingleAsync();
        Assert.Equal("TaskCreated", auditEvent.Action);
        Assert.Equal("CitizenTask", auditEvent.EntityType);
        Assert.Equal(result!.CitizenTaskId, auditEvent.EntityId);
        Assert.Equal(citizen.CitizenId, auditEvent.CitizenId);
        Assert.Equal(department.DepartmentId, auditEvent.DepartmentId);
        Assert.Equal("user-2", auditEvent.UserId);
        Assert.Equal("Bent Jensen", auditEvent.UserDisplayNameSnapshot);
        Assert.Equal(ShiftType.Day, auditEvent.ShiftType);
    }

    [Fact]
    public async Task CompleteAsync_CompletesTask_WhenTaskExistsInDepartment()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var task = new CitizenTask
        {
            CitizenId = citizen.CitizenId,
            DepartmentId = department.DepartmentId,
            ShiftType = ShiftType.Day,
            Title = "Husk væskeskema",
            Description = "Beskrivelse",
            CreatedByUserId = "user-1",
            CreatedAtUtc = DateTime.UtcNow,
            IsCompleted = false
        };
        context.CitizenTasks.Add(task);
        await context.SaveChangesAsync();

        var service = new CitizenTaskService(context);

        var result = await service.CompleteAsync(
            citizen.CitizenId,
            task.CitizenTaskId,
            department.DepartmentId,
            "user-2");

        Assert.NotNull(result);
        Assert.True(result!.IsCompleted);
        Assert.NotNull(result.CompletedAtUtc);
        Assert.Equal("user-2", result.CompletedByUserId);

        var dbTask = await context.CitizenTasks.SingleAsync();
        Assert.True(dbTask.IsCompleted);
        Assert.Equal("user-2", dbTask.CompletedByUserId);
    }

    [Fact]
    public async Task CompleteAsync_ReturnsNull_WhenTaskNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = depA.DepartmentId };
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var service = new CitizenTaskService(context);

        var result = await service.CompleteAsync(citizen.CitizenId, 123, depB.DepartmentId, "user-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task CompleteAsync_WritesAuditEvent_WhenAuditServiceIsInjected()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var task = new CitizenTask
        {
            CitizenId = citizen.CitizenId,
            DepartmentId = department.DepartmentId,
            ShiftType = ShiftType.Night,
            Title = "Tjek naturo",
            Description = "Beskrivelse",
            CreatedByUserId = "user-1",
            CreatedAtUtc = DateTime.UtcNow,
            IsCompleted = false
        };
        context.CitizenTasks.Add(task);
        await context.SaveChangesAsync();

        var auditService = new AuditService(context);
        var service = new CitizenTaskService(context, auditService);

        var result = await service.CompleteAsync(
            citizen.CitizenId,
            task.CitizenTaskId,
            department.DepartmentId,
            "user-3",
            userDisplayNameSnapshot: "Anna Jensen");

        Assert.NotNull(result);

        var auditEvent = await context.AuditEvents.SingleAsync();
        Assert.Equal("TaskCompleted", auditEvent.Action);
        Assert.Equal("CitizenTask", auditEvent.EntityType);
        Assert.Equal(task.CitizenTaskId, auditEvent.EntityId);
        Assert.Equal(citizen.CitizenId, auditEvent.CitizenId);
        Assert.Equal(department.DepartmentId, auditEvent.DepartmentId);
        Assert.Equal("user-3", auditEvent.UserId);
        Assert.Equal("Anna Jensen", auditEvent.UserDisplayNameSnapshot);
        Assert.Equal(ShiftType.Night, auditEvent.ShiftType);
    }

    private static CreateCitizenTaskRequest CreateValidRequest()
    {
        return new CreateCitizenTaskRequest
        {
            Title = "Husk væskeskema",
            Description = "Følg op senere.",
            ShiftType = ShiftType.Day
        };
    }

    private static async Task<(Department Department, Citizen Citizen)> CreateCitizenInDepartmentAsync(AppDbContext context)
    {
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = department.DepartmentId };
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        return (department, citizen);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
