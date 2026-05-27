using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class OverlapServiceTests
{
    [Fact]
    public async Task GetOverlapAsync_ReturnsNull_WhenDepartmentDoesNotExist()
    {
        using var context = CreateContext();
        var service = new OverlapService(context);

        var result = await service.GetOverlapAsync(123, ShiftType.Day);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOverlapAsync_ReturnsCitizensForDepartmentOnly()
    {
        using var context = CreateContext();
        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        context.Citizens.AddRange(
            new Citizen { Name = "Anna", DepartmentId = depA.DepartmentId, Status = CitizenStatus.Green },
            new Citizen { Name = "Bent", DepartmentId = depB.DepartmentId, Status = CitizenStatus.Red });
        await context.SaveChangesAsync();

        var service = new OverlapService(context);

        var result = await service.GetOverlapAsync(depA.DepartmentId, ShiftType.Evening);

        Assert.NotNull(result);
        Assert.Equal(depA.DepartmentId, result!.DepartmentId);
        Assert.Equal("A", result.DepartmentName);
        Assert.Equal(ShiftType.Evening, result.ActiveShiftType);
        Assert.Equal("Aftenvagt", result.ActiveShiftDisplayName);
        Assert.Single(result.Citizens);
        Assert.Equal("Anna", result.Citizens.Single().Name);
    }

    [Fact]
    public async Task GetOverlapAsync_IncludesMedicationPnAuditNotesAndOpenTasks()
    {
        using var context = CreateContext();
        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);

        context.FixedMedications.Add(new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil",
            PlannedAt = new DateTime(2026, 5, 25, 8, 0, 0, DateTimeKind.Utc),
            ScheduleDescription = "Fast tidspunkt",
            IsActive = true,
            IsGiven = true,
            GivenAt = new DateTime(2026, 5, 25, 8, 5, 0, DateTimeKind.Utc),
            GivenByUserId = "user-1"
        });

        context.PnMedications.Add(new PnMedication
        {
            CitizenId = citizen.CitizenId,
            DepartmentId = department.DepartmentId,
            ShiftType = ShiftType.Day,
            MedicineName = "PN Panodil",
            Dose = "1 tablet",
            Reason = "Smerter",
            GivenAtUtc = new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc),
            GivenByUserId = "user-1",
            CreatedAtUtc = new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc)
        });

        context.Notes.Add(new Note
        {
            CitizenId = citizen.CitizenId,
            DepartmentId = department.DepartmentId,
            ShiftType = ShiftType.Day,
            Content = "Borger virker rolig.",
            CreatedByUserId = "user-1",
            CreatedAtUtc = new DateTime(2026, 5, 25, 10, 0, 0, DateTimeKind.Utc)
        });

        context.CitizenTasks.AddRange(
            new CitizenTask
            {
                CitizenId = citizen.CitizenId,
                DepartmentId = department.DepartmentId,
                ShiftType = ShiftType.Day,
                Title = "Åben opgave",
                Description = "Følg op.",
                CreatedByUserId = "user-1",
                CreatedAtUtc = new DateTime(2026, 5, 25, 10, 30, 0, DateTimeKind.Utc),
                IsCompleted = false
            },
            new CitizenTask
            {
                CitizenId = citizen.CitizenId,
                DepartmentId = department.DepartmentId,
                ShiftType = ShiftType.Day,
                Title = "Lukket opgave",
                Description = "Færdig.",
                CreatedByUserId = "user-1",
                CreatedAtUtc = new DateTime(2026, 5, 25, 8, 30, 0, DateTimeKind.Utc),
                IsCompleted = true,
                CompletedAtUtc = new DateTime(2026, 5, 25, 9, 30, 0, DateTimeKind.Utc),
                CompletedByUserId = "user-2"
            });

        context.AuditEvents.Add(new AuditEvent
        {
            CitizenId = citizen.CitizenId,
            DepartmentId = department.DepartmentId,
            EntityType = "Note",
            EntityId = 1,
            Action = "NoteCreated",
            Description = "Note oprettet på borger.",
            UserId = "user-1",
            UserDisplayNameSnapshot = "Test Bruger",
            ShiftType = ShiftType.Day,
            CreatedAtUtc = new DateTime(2026, 5, 25, 10, 0, 0, DateTimeKind.Utc)
        });

        await context.SaveChangesAsync();

        var service = new OverlapService(context);

        var result = await service.GetOverlapAsync(department.DepartmentId, ShiftType.Day);

        Assert.NotNull(result);
        var dto = Assert.Single(result!.Citizens);
        Assert.Single(dto.FixedMedications);
        Assert.Single(dto.RecentPnMedications);
        Assert.Single(dto.ActiveNotes);
        Assert.Single(dto.OpenTasks);
        Assert.Single(dto.RecentAuditEvents);
        Assert.Equal("Åben opgave", dto.OpenTasks.Single().Title);
        Assert.Equal("Borger virker rolig.", dto.ActiveNotes.Single().Content);
    }

    private static async Task<(Department Department, Citizen Citizen)> CreateCitizenInDepartmentAsync(AppDbContext context)
    {
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var citizen = new Citizen
        {
            Name = "Anna",
            DepartmentId = department.DepartmentId,
            Status = CitizenStatus.Yellow
        };
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
