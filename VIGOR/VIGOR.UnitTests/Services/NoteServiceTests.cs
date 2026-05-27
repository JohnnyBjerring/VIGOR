using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class NoteServiceTests
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

        var service = new NoteService(context);

        var result = await service.GetForCitizenAsync(citizen.CitizenId, depB.DepartmentId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetForCitizenAsync_ReturnsNotesOrderedByCreatedAtDescending()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);

        context.Notes.AddRange(
            new Note
            {
                CitizenId = citizen.CitizenId,
                DepartmentId = department.DepartmentId,
                ShiftType = ShiftType.Day,
                Content = "Første note",
                CreatedByUserId = "user-1",
                CreatedAtUtc = new DateTime(2026, 5, 25, 8, 0, 0, DateTimeKind.Utc)
            },
            new Note
            {
                CitizenId = citizen.CitizenId,
                DepartmentId = department.DepartmentId,
                ShiftType = ShiftType.Evening,
                Content = "Anden note",
                CreatedByUserId = "user-1",
                CreatedAtUtc = new DateTime(2026, 5, 25, 20, 0, 0, DateTimeKind.Utc)
            });

        await context.SaveChangesAsync();

        var service = new NoteService(context);

        var result = await service.GetForCitizenAsync(citizen.CitizenId, department.DepartmentId);

        Assert.NotNull(result);
        Assert.Collection(
            result!,
            first => Assert.Equal("Anden note", first.Content),
            second => Assert.Equal("Første note", second.Content));
    }

    [Fact]
    public async Task CreateAsync_CreatesNote_WhenRequestIsValid()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var service = new NoteService(context);

        var request = new CreateNoteRequest
        {
            Content = "Borger virker rolig efter aftensmad.",
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
        Assert.Equal("Borger virker rolig efter aftensmad.", result.Content);
        Assert.Equal("user-1", result.CreatedByUserId);

        var dbNote = await context.Notes.SingleAsync();
        Assert.Equal("Borger virker rolig efter aftensmad.", dbNote.Content);
        Assert.Equal("user-1", dbNote.CreatedByUserId);
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

        var service = new NoteService(context);
        var request = CreateValidRequest();

        var result = await service.CreateAsync(citizen.CitizenId, depB.DepartmentId, "user-1", request);

        Assert.Null(result);
        Assert.Empty(context.Notes);
    }

    [Fact]
    public async Task CreateAsync_ThrowsArgumentException_WhenContentMissing()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var service = new NoteService(context);
        var request = CreateValidRequest();
        request.Content = "   ";

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(citizen.CitizenId, department.DepartmentId, "user-1", request));
    }

    [Fact]
    public async Task CreateAsync_ThrowsArgumentException_WhenShiftTypeInvalid()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var service = new NoteService(context);
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
        var service = new NoteService(context, auditService);

        var result = await service.CreateAsync(
            citizen.CitizenId,
            department.DepartmentId,
            "user-2",
            CreateValidRequest(),
            userDisplayNameSnapshot: "Bent Jensen");

        Assert.NotNull(result);

        var auditEvent = await context.AuditEvents.SingleAsync();
        Assert.Equal("NoteCreated", auditEvent.Action);
        Assert.Equal("Note", auditEvent.EntityType);
        Assert.Equal(result!.NoteId, auditEvent.EntityId);
        Assert.Equal(citizen.CitizenId, auditEvent.CitizenId);
        Assert.Equal(department.DepartmentId, auditEvent.DepartmentId);
        Assert.Equal("user-2", auditEvent.UserId);
        Assert.Equal("Bent Jensen", auditEvent.UserDisplayNameSnapshot);
        Assert.Equal(ShiftType.Day, auditEvent.ShiftType);
    }

    private static CreateNoteRequest CreateValidRequest()
    {
        return new CreateNoteRequest
        {
            Content = "Borger virker rolig.",
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
