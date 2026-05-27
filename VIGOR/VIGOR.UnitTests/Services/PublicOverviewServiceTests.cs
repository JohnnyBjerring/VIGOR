using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class PublicOverviewServiceTests
{
    [Fact]
    public async Task GetPublicOverviewAsync_ReturnsAnonymousStatusCounts()
    {
        using var context = CreateContext();
        var department = new Department { Name = "Afdeling A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        context.Citizens.AddRange(
            new Citizen { Name = "Jens Hansen", DepartmentId = department.DepartmentId, Status = CitizenStatus.Green },
            new Citizen { Name = "Anna Nielsen", DepartmentId = department.DepartmentId, Status = CitizenStatus.Yellow },
            new Citizen { Name = "Ole Jensen", DepartmentId = department.DepartmentId, Status = CitizenStatus.Red });
        await context.SaveChangesAsync();

        var service = new PublicOverviewService(context);

        var result = await service.GetPublicOverviewAsync();

        Assert.Equal(3, result.TotalCitizenCount);
        Assert.Equal(1, result.GreenCount);
        Assert.Equal(1, result.YellowCount);
        Assert.Equal(1, result.RedCount);
        Assert.Equal(3, result.Citizens.Count);
        Assert.Equal("Borger 1", result.Citizens.First().DisplayLabel);
        Assert.Equal(CitizenStatus.Red, result.Citizens.First().Status);
    }

    [Fact]
    public async Task GetPublicOverviewAsync_DoesNotExposeSensitiveFieldsOrText()
    {
        using var context = CreateContext();
        var department = new Department { Name = "Hemmelig Afdeling" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var citizen = new Citizen
        {
            Name = "Jens Hansen",
            DepartmentId = department.DepartmentId,
            Status = CitizenStatus.Red
        };
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        context.FixedMedications.Add(new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil hemmelig medicin",
            PlannedAt = new DateTime(2026, 5, 25, 8, 0, 0, DateTimeKind.Utc),
            ScheduleDescription = "Dagligt",
            IsActive = true
        });

        context.PnMedications.Add(new PnMedication
        {
            CitizenId = citizen.CitizenId,
            DepartmentId = department.DepartmentId,
            ShiftType = ShiftType.Day,
            MedicineName = "PN hemmelig medicin",
            Dose = "1 tablet",
            Reason = "Smerter",
            GivenAtUtc = new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc),
            GivenByUserId = "sensitive-user-id",
            CreatedAtUtc = new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc)
        });

        context.Notes.Add(new Note
        {
            CitizenId = citizen.CitizenId,
            DepartmentId = department.DepartmentId,
            ShiftType = ShiftType.Day,
            Content = "Fortrolig note om borger",
            CreatedByUserId = "sensitive-user-id",
            CreatedAtUtc = new DateTime(2026, 5, 25, 10, 0, 0, DateTimeKind.Utc)
        });

        context.CitizenTasks.Add(new CitizenTask
        {
            CitizenId = citizen.CitizenId,
            DepartmentId = department.DepartmentId,
            ShiftType = ShiftType.Day,
            Title = "Fortrolig opgave",
            Description = "Fortrolig opgavetekst",
            CreatedByUserId = "sensitive-user-id",
            CreatedAtUtc = new DateTime(2026, 5, 25, 10, 30, 0, DateTimeKind.Utc),
            IsCompleted = false
        });

        await context.SaveChangesAsync();

        var service = new PublicOverviewService(context);
        var result = await service.GetPublicOverviewAsync();
        var json = JsonSerializer.Serialize(result);

        Assert.DoesNotContain("Jens", json);
        Assert.DoesNotContain("Hansen", json);
        Assert.DoesNotContain("Hemmelig Afdeling", json);
        Assert.DoesNotContain("Panodil", json);
        Assert.DoesNotContain("PN hemmelig", json);
        Assert.DoesNotContain("Fortrolig note", json);
        Assert.DoesNotContain("Fortrolig opgave", json);
        Assert.DoesNotContain("sensitive-user-id", json);
        Assert.DoesNotContain("CitizenId", json);
        Assert.DoesNotContain("DepartmentId", json);
        Assert.Contains("Borger 1", json);
        Assert.Equal(CitizenStatus.Red, result.Citizens.Single().Status);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
