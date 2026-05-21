using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class PnMedicationServiceTests
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

        var service = new PnMedicationService(context);

        var result = await service.GetForCitizenAsync(citizen.CitizenId, depB.DepartmentId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetForCitizenAsync_ReturnsRegistrationsOrderedByGivenAtDescending()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);

        context.PnMedications.AddRange(
            new PnMedication
            {
                CitizenId = citizen.CitizenId,
                DepartmentId = department.DepartmentId,
                ShiftType = ShiftType.Day,
                MedicineName = "Panodil",
                Dose = "1 tablet",
                Reason = "Smerter",
                GivenAtUtc = new DateTime(2026, 5, 18, 8, 0, 0, DateTimeKind.Utc),
                GivenByUserId = "user-1",
                CreatedAtUtc = new DateTime(2026, 5, 18, 8, 1, 0, DateTimeKind.Utc)
            },
            new PnMedication
            {
                CitizenId = citizen.CitizenId,
                DepartmentId = department.DepartmentId,
                ShiftType = ShiftType.Evening,
                MedicineName = "Melatonin",
                Dose = "2 mg",
                Reason = "Søvn",
                GivenAtUtc = new DateTime(2026, 5, 18, 20, 0, 0, DateTimeKind.Utc),
                GivenByUserId = "user-1",
                CreatedAtUtc = new DateTime(2026, 5, 18, 20, 1, 0, DateTimeKind.Utc)
            });

        await context.SaveChangesAsync();

        var service = new PnMedicationService(context);

        var result = await service.GetForCitizenAsync(citizen.CitizenId, department.DepartmentId);

        Assert.NotNull(result);
        Assert.Collection(
            result!,
            first => Assert.Equal("Melatonin", first.MedicineName),
            second => Assert.Equal("Panodil", second.MedicineName));
    }

    [Fact]
    public async Task RegisterAsync_CreatesPnMedicationRegistration_WhenRequestIsValid()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var givenAt = new DateTime(2026, 5, 18, 10, 30, 0, DateTimeKind.Utc);
        var service = new PnMedicationService(context);

        var request = new RegisterPnMedicationRequest
        {
            MedicineName = "Panodil",
            Dose = "1 tablet",
            Reason = "Smerter",
            GivenAt = givenAt,
            ShiftType = ShiftType.Day
        };

        var result = await service.RegisterAsync(
            citizen.CitizenId,
            department.DepartmentId,
            "user-1",
            request);

        Assert.NotNull(result);
        Assert.Equal(citizen.CitizenId, result!.CitizenId);
        Assert.Equal(department.DepartmentId, result.DepartmentId);
        Assert.Equal(ShiftType.Day, result.ShiftType);
        Assert.Equal("Dagvagt", result.ShiftDisplayName);
        Assert.Equal("Panodil", result.MedicineName);
        Assert.Equal("1 tablet", result.Dose);
        Assert.Equal("Smerter", result.Reason);
        Assert.Equal(givenAt, result.GivenAtUtc);
        Assert.Equal("user-1", result.GivenByUserId);

        var dbRegistration = await context.PnMedications.SingleAsync();
        Assert.Equal("Panodil", dbRegistration.MedicineName);
        Assert.Equal("user-1", dbRegistration.GivenByUserId);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsNull_WhenCitizenNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = depA.DepartmentId };
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var service = new PnMedicationService(context);
        var request = CreateValidRequest();

        var result = await service.RegisterAsync(citizen.CitizenId, depB.DepartmentId, "user-1", request);

        Assert.Null(result);
        Assert.Empty(context.PnMedications);
    }

    [Theory]
    [InlineData("", "1 tablet", "Smerter")]
    [InlineData("Panodil", "", "Smerter")]
    [InlineData("Panodil", "1 tablet", "")]
    public async Task RegisterAsync_ThrowsArgumentException_WhenRequiredTextIsMissing(
        string medicineName,
        string dose,
        string reason)
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var service = new PnMedicationService(context);
        var request = new RegisterPnMedicationRequest
        {
            MedicineName = medicineName,
            Dose = dose,
            Reason = reason,
            ShiftType = ShiftType.Day
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegisterAsync(citizen.CitizenId, department.DepartmentId, "user-1", request));
    }

    [Fact]
    public async Task RegisterAsync_ThrowsArgumentException_WhenShiftTypeIsInvalid()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var service = new PnMedicationService(context);
        var request = CreateValidRequest();
        request.ShiftType = (ShiftType)999;

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegisterAsync(citizen.CitizenId, department.DepartmentId, "user-1", request));
    }

    [Fact]
    public async Task RegisterAsync_ThrowsArgumentException_WhenGivenAtInvalid()
    {
        using var context = CreateContext();

        var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
        var service = new PnMedicationService(context);
        var request = CreateValidRequest();
        request.GivenAt = DateTime.MinValue;

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegisterAsync(citizen.CitizenId, department.DepartmentId, "user-1", request));
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
