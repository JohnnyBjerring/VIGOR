using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class FixedMedicationServiceTests
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

        var service = new FixedMedicationService(context);

        var result = await service.GetForCitizenAsync(citizen.CitizenId, depB.DepartmentId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetForCitizenAsync_ReturnsEmpty_WhenNoMedication()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);

        var service = new FixedMedicationService(context);

        var result = await service.GetForCitizenAsync(citizen.CitizenId, department.DepartmentId);

        Assert.NotNull(result);
        Assert.Empty(result!);
    }

    [Fact]
    public async Task GetForCitizenAsync_ReturnsMedicationOrderedByPlannedAt_WhenMedicationExists()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);

        var laterMedication = new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Aftenmedicin",
            PlannedAt = DateTime.UtcNow.Date.AddHours(20)
        };

        var earlierMedication = new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Morgenmedicin",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8)
        };

        context.FixedMedications.AddRange(laterMedication, earlierMedication);
        await context.SaveChangesAsync();

        var service = new FixedMedicationService(context);

        var result = await service.GetForCitizenAsync(citizen.CitizenId, department.DepartmentId);

        Assert.NotNull(result);
        Assert.Collection(
            result!,
            first => Assert.Equal("Morgenmedicin", first.Name),
            second => Assert.Equal("Aftenmedicin", second.Name));
    }

    [Fact]
    public async Task GiveAsync_SetsGivenAndTraceability_WhenValidWithoutExplicitGivenAt()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var medication = await CreateFixedMedicationAsync(context, citizen.CitizenId);

        var service = new FixedMedicationService(context);

        var result = await service.GiveAsync(
            citizen.CitizenId,
            medication.FixedMedicationId,
            department.DepartmentId,
            "user-1",
            null);

        Assert.NotNull(result);
        Assert.True(result!.IsGiven);
        Assert.Equal("user-1", result.GivenByUserId);
        Assert.NotNull(result.GivenAt);

        var dbMedication = await context.FixedMedications.FindAsync(medication.FixedMedicationId);
        Assert.NotNull(dbMedication);
        Assert.True(dbMedication!.IsGiven);
        Assert.Equal("user-1", dbMedication.GivenByUserId);
        Assert.NotNull(dbMedication.GivenAt);
    }

    [Fact]
    public async Task GiveAsync_UsesProvidedGivenAt_WhenValidGivenAtProvided()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var medication = await CreateFixedMedicationAsync(context, citizen.CitizenId);

        var givenAt = new DateTime(2026, 4, 27, 10, 30, 0, DateTimeKind.Utc);
        var service = new FixedMedicationService(context);

        var result = await service.GiveAsync(
            citizen.CitizenId,
            medication.FixedMedicationId,
            department.DepartmentId,
            "user-1",
            givenAt);

        Assert.NotNull(result);
        Assert.True(result!.IsGiven);
        Assert.Equal("user-1", result.GivenByUserId);
        Assert.Equal(givenAt, result.GivenAt);

        var dbMedication = await context.FixedMedications.FindAsync(medication.FixedMedicationId);
        Assert.NotNull(dbMedication);
        Assert.Equal(givenAt, dbMedication!.GivenAt);
    }

    [Fact]
    public async Task GiveAsync_ThrowsInvalidOperationException_WhenMedicationIsInactive()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var medication = await CreateFixedMedicationAsync(context, citizen.CitizenId);
        medication.IsActive = false;
        await context.SaveChangesAsync();

        var service = new FixedMedicationService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GiveAsync(
                citizen.CitizenId,
                medication.FixedMedicationId,
                department.DepartmentId,
                "user-1",
                null));

        Assert.Equal("Medicinplanen er inaktiv og kan ikke registreres som givet.", ex.Message);

        var dbMedication = await context.FixedMedications.FindAsync(medication.FixedMedicationId);
        Assert.NotNull(dbMedication);
        Assert.False(dbMedication!.IsGiven);
        Assert.Null(dbMedication.GivenAt);
        Assert.Null(dbMedication.GivenByUserId);
    }

    [Fact]
    public async Task CancelGivenAsync_ClearsOnlyCurrentRegistrationAndKeepsMedicationPlan_WhenMedicationWasGiven()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);

        var plannedAt = new DateTime(2000, 1, 1, 14, 30, 0);
        var medication = new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil 1g",
            PlannedAt = plannedAt,
            ScheduleDescription = "Fast tidspunkt",
            IsActive = true,
            IsGiven = true,
            GivenAt = new DateTime(2026, 4, 27, 10, 30, 0, DateTimeKind.Utc),
            GivenByUserId = "user-1"
        };

        context.FixedMedications.Add(medication);
        await context.SaveChangesAsync();

        var service = new FixedMedicationService(context);

        var result = await service.CancelGivenAsync(
            citizen.CitizenId,
            medication.FixedMedicationId,
            department.DepartmentId,
            "user-2");

        Assert.NotNull(result);
        Assert.False(result!.IsGiven);
        Assert.Null(result.GivenAt);
        Assert.Null(result.GivenByUserId);

        Assert.Equal("Panodil 1g", result.Name);
        Assert.Equal(plannedAt.Hour, result.PlannedAt.Hour);
        Assert.Equal(plannedAt.Minute, result.PlannedAt.Minute);
        Assert.Equal("Fast tidspunkt", result.ScheduleDescription);
        Assert.True(result.IsActive);

        var dbMedication = await context.FixedMedications.FindAsync(medication.FixedMedicationId);
        Assert.NotNull(dbMedication);
        Assert.Equal("Panodil 1g", dbMedication!.Name);
        Assert.Equal(plannedAt.Hour, dbMedication.PlannedAt.Hour);
        Assert.Equal(plannedAt.Minute, dbMedication.PlannedAt.Minute);
        Assert.Equal("Fast tidspunkt", dbMedication.ScheduleDescription);
        Assert.True(dbMedication.IsActive);
        Assert.False(dbMedication.IsGiven);
        Assert.Null(dbMedication.GivenAt);
        Assert.Null(dbMedication.GivenByUserId);
    }

    [Fact]
    public async Task GiveAsync_UpdatesExistingGivenRegistration_WhenAlreadyGiven()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);

        var oldGivenAt = new DateTime(2026, 4, 20, 8, 0, 0, DateTimeKind.Utc);
        var newGivenAt = new DateTime(2026, 4, 27, 10, 30, 0, DateTimeKind.Utc);

        var medication = new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8),
            IsGiven = true,
            GivenAt = oldGivenAt,
            GivenByUserId = "user-1"
        };

        context.FixedMedications.Add(medication);
        await context.SaveChangesAsync();

        var service = new FixedMedicationService(context);

        var result = await service.GiveAsync(
            citizen.CitizenId,
            medication.FixedMedicationId,
            department.DepartmentId,
            "user-2",
            newGivenAt);

        Assert.NotNull(result);
        Assert.True(result!.IsGiven);
        Assert.Equal(newGivenAt, result.GivenAt);
        Assert.Equal("user-2", result.GivenByUserId);

        var dbMedication = await context.FixedMedications.FindAsync(medication.FixedMedicationId);
        Assert.NotNull(dbMedication);
        Assert.True(dbMedication!.IsGiven);
        Assert.Equal(newGivenAt, dbMedication.GivenAt);
        Assert.Equal("user-2", dbMedication.GivenByUserId);
    }

    [Fact]
    public async Task CancelGivenAsync_ClearsGivenState_WhenMedicationWasGiven()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);

        var medication = new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8),
            IsGiven = true,
            GivenAt = DateTime.UtcNow,
            GivenByUserId = "user-1"
        };

        context.FixedMedications.Add(medication);
        await context.SaveChangesAsync();

        var service = new FixedMedicationService(context);

        var result = await service.CancelGivenAsync(
            citizen.CitizenId,
            medication.FixedMedicationId,
            department.DepartmentId,
            "user-2");

        Assert.NotNull(result);
        Assert.False(result!.IsGiven);
        Assert.Null(result.GivenAt);
        Assert.Null(result.GivenByUserId);

        var dbMedication = await context.FixedMedications.FindAsync(medication.FixedMedicationId);
        Assert.NotNull(dbMedication);
        Assert.False(dbMedication!.IsGiven);
        Assert.Null(dbMedication.GivenAt);
        Assert.Null(dbMedication.GivenByUserId);
    }

    [Fact]
    public async Task CancelGivenAsync_ReturnsMedicationUnchanged_WhenMedicationWasNotGiven()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var medication = await CreateFixedMedicationAsync(context, citizen.CitizenId);

        var service = new FixedMedicationService(context);

        var result = await service.CancelGivenAsync(
            citizen.CitizenId,
            medication.FixedMedicationId,
            department.DepartmentId,
            "user-2");

        Assert.NotNull(result);
        Assert.False(result!.IsGiven);
        Assert.Null(result.GivenAt);
        Assert.Null(result.GivenByUserId);
    }

    [Fact]
    public async Task GiveAsync_ThrowsArgumentException_WhenGivenByUserIdIsMissing()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var medication = await CreateFixedMedicationAsync(context, citizen.CitizenId);

        var service = new FixedMedicationService(context);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GiveAsync(
                citizen.CitizenId,
                medication.FixedMedicationId,
                department.DepartmentId,
                "",
                null));
    }

    [Fact]
    public async Task CancelGivenAsync_ThrowsArgumentException_WhenCancelledByUserIdIsMissing()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var medication = await CreateFixedMedicationAsync(context, citizen.CitizenId);

        var service = new FixedMedicationService(context);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CancelGivenAsync(
                citizen.CitizenId,
                medication.FixedMedicationId,
                department.DepartmentId,
                ""));
    }

    [Fact]
    public async Task GiveAsync_ThrowsArgumentException_WhenGivenAtIsDefault()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var medication = await CreateFixedMedicationAsync(context, citizen.CitizenId);

        var service = new FixedMedicationService(context);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GiveAsync(
                citizen.CitizenId,
                medication.FixedMedicationId,
                department.DepartmentId,
                "user-1",
                DateTime.MinValue));
    }

    [Fact]
    public async Task GiveAsync_ReturnsNull_WhenCitizenNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = depA.DepartmentId };
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var medication = await CreateFixedMedicationAsync(context, citizen.CitizenId);

        var service = new FixedMedicationService(context);

        var result = await service.GiveAsync(
            citizen.CitizenId,
            medication.FixedMedicationId,
            depB.DepartmentId,
            "user-1",
            null);

        Assert.Null(result);
    }

    [Fact]
    public async Task CancelGivenAsync_ReturnsNull_WhenCitizenNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = depA.DepartmentId };
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var medication = await CreateFixedMedicationAsync(context, citizen.CitizenId);

        var service = new FixedMedicationService(context);

        var result = await service.CancelGivenAsync(
            citizen.CitizenId,
            medication.FixedMedicationId,
            depB.DepartmentId,
            "user-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GiveAsync_ReturnsNull_WhenMedicationNotFound()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);

        var service = new FixedMedicationService(context);

        var result = await service.GiveAsync(
            citizen.CitizenId,
            999,
            department.DepartmentId,
            "user-1",
            null);

        Assert.Null(result);
    }

    [Fact]
    public async Task CancelGivenAsync_ReturnsNull_WhenMedicationNotFound()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);

        var service = new FixedMedicationService(context);

        var result = await service.CancelGivenAsync(
            citizen.CitizenId,
            999,
            department.DepartmentId,
            "user-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePlanAsync_UpdatesNameTimeScheduleAndActive_WhenValid()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var medication = await CreateFixedMedicationAsync(context, citizen.CitizenId);

        var service = new FixedMedicationService(context);
        var request = new UpdateFixedMedicationPlanRequest
        {
            Name = "Panodil 500mg",
            PlannedAt = new DateTime(2026, 4, 29, 14, 45, 0),
            ScheduleDescription = "Dagligt",
            IsActive = false
        };

        var result = await service.UpdatePlanAsync(
            citizen.CitizenId,
            medication.FixedMedicationId,
            department.DepartmentId,
            request);

        Assert.NotNull(result);
        Assert.Equal("Panodil 500mg", result!.Name);
        Assert.Equal(14, result.PlannedAt.Hour);
        Assert.Equal(45, result.PlannedAt.Minute);
        Assert.Equal("Dagligt", result.ScheduleDescription);
        Assert.False(result.IsActive);

        var dbMedication = await context.FixedMedications.FindAsync(medication.FixedMedicationId);
        Assert.NotNull(dbMedication);
        Assert.Equal("Panodil 500mg", dbMedication!.Name);
        Assert.Equal(14, dbMedication.PlannedAt.Hour);
        Assert.Equal(45, dbMedication.PlannedAt.Minute);
        Assert.Equal("Dagligt", dbMedication.ScheduleDescription);
        Assert.False(dbMedication.IsActive);
    }

    [Fact]
    public async Task UpdatePlanAsync_ReturnsNull_WhenCitizenNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = depA.DepartmentId };
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var medication = await CreateFixedMedicationAsync(context, citizen.CitizenId);
        var service = new FixedMedicationService(context);

        var result = await service.UpdatePlanAsync(
            citizen.CitizenId,
            medication.FixedMedicationId,
            depB.DepartmentId,
            new UpdateFixedMedicationPlanRequest
            {
                Name = "Panodil",
                PlannedAt = new DateTime(2026, 4, 29, 8, 0, 0),
                ScheduleDescription = "Dagligt",
                IsActive = true
            });

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePlanAsync_ThrowsArgumentException_WhenNameIsMissing()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var medication = await CreateFixedMedicationAsync(context, citizen.CitizenId);

        var service = new FixedMedicationService(context);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdatePlanAsync(
                citizen.CitizenId,
                medication.FixedMedicationId,
                department.DepartmentId,
                new UpdateFixedMedicationPlanRequest
                {
                    Name = "",
                    PlannedAt = new DateTime(2026, 4, 29, 8, 0, 0),
                    ScheduleDescription = "Dagligt",
                    IsActive = true
                }));
    }

    private static async Task<(Department Department, Citizen Citizen)> CreateCitizenInDepartmentAsync(AppDbContext context)
    {
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        return (department, citizen);
    }

    private static async Task<FixedMedication> CreateFixedMedicationAsync(AppDbContext context, int citizenId)
    {
        var medication = new FixedMedication
        {
            CitizenId = citizenId,
            Name = "Panodil",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8)
        };

        context.FixedMedications.Add(medication);
        await context.SaveChangesAsync();

        return medication;
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
