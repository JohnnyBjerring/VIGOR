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

public class CitizensApiControllerTests
{
    [Fact]
    public async Task GetCitizens_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetCitizens(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetCitizens_ReturnsOk_ForEmployeeWithDepartment()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);
        context.Citizens.Add(new Citizen { Name = "Citizen", DepartmentId = department.DepartmentId });
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GetCitizens(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var citizens = Assert.IsAssignableFrom<IEnumerable<Citizen>>(ok.Value);
        var citizen = Assert.Single(citizens);

        Assert.Equal(department.DepartmentId, citizen.DepartmentId);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var request = new UpdateCitizenStatusRequest { Status = CitizenStatus.Red };

        var result = await controller.UpdateStatus(1, request, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "invalid-user");

        var request = new UpdateCitizenStatusRequest { Status = CitizenStatus.Red };

        var result = await controller.UpdateStatus(1, request, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsBadRequest_WhenStatusInvalid()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var request = new UpdateCitizenStatusRequest { Status = (CitizenStatus)999 };

        var result = await controller.UpdateStatus(1, request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsNotFound_WhenCitizenNotFound()
    {
        using var context = CreateContext();

        var (_, employee) = await CreateDepartmentAndEmployeeAsync(context);
        var controller = CreateController(context, employee.IdentityUserId);

        var request = new UpdateCitizenStatusRequest { Status = CitizenStatus.Red };

        var result = await controller.UpdateStatus(999, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsOk_WhenStatusUpdated()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId,
            Status = CitizenStatus.Green
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);
        var request = new UpdateCitizenStatusRequest { Status = CitizenStatus.Red };

        var result = await controller.UpdateStatus(citizen.CitizenId, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var updatedCitizen = Assert.IsType<Citizen>(ok.Value);

        Assert.Equal(CitizenStatus.Red, updatedCitizen.Status);
    }

    [Fact]
    public async Task GetFixedMedications_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetFixedMedications(1, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetFixedMedications_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "invalid-user");

        var result = await controller.GetFixedMedications(1, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetFixedMedications_ReturnsNotFound_WhenCitizenNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = depB.DepartmentId };
        var employee = new Employee
        {
            IdentityUserId = "user-1",
            Name = "Employee",
            DepartmentId = depA.DepartmentId
        };

        context.Citizens.Add(citizen);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        context.FixedMedications.Add(new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8)
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GetFixedMedications(citizen.CitizenId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetFixedMedications_ReturnsOk_ForCitizenInDepartment()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        context.FixedMedications.Add(new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8)
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GetFixedMedications(citizen.CitizenId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var medications = Assert.IsAssignableFrom<IReadOnlyList<FixedMedicationDto>>(ok.Value);

        Assert.Single(medications);
    }

    [Fact]
    public async Task GiveFixedMedication_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GiveFixedMedication(
            1,
            1,
            new RegisterFixedMedicationGivenRequest(),
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GiveFixedMedication_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "invalid-user");

        var result = await controller.GiveFixedMedication(
            1,
            1,
            new RegisterFixedMedicationGivenRequest(),
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GiveFixedMedication_ReturnsNotFound_WhenMedicationNotFound()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GiveFixedMedication(
            citizen.CitizenId,
            999,
            new RegisterFixedMedicationGivenRequest(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GiveFixedMedication_ReturnsConflict_WhenMedicationIsInactive()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var medication = new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8),
            IsActive = false,
            IsGiven = false,
            GivenAt = null,
            GivenByUserId = null
        };

        context.FixedMedications.Add(medication);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GiveFixedMedication(
            citizen.CitizenId,
            medication.FixedMedicationId,
            new RegisterFixedMedicationGivenRequest(),
            CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Medicinplanen er inaktiv og kan ikke registreres som givet.", conflict.Value);

        var dbMedication = await context.FixedMedications.FindAsync(medication.FixedMedicationId);
        Assert.NotNull(dbMedication);
        Assert.False(dbMedication!.IsGiven);
        Assert.Null(dbMedication.GivenAt);
        Assert.Null(dbMedication.GivenByUserId);
    }

    [Fact]
    public async Task GiveFixedMedication_ReturnsOk_WhenAlreadyGivenAndUpdatesRegistration()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var oldGivenAt = new DateTime(2026, 4, 20, 8, 0, 0, DateTimeKind.Utc);
        var newGivenAt = new DateTime(2026, 4, 27, 10, 30, 0, DateTimeKind.Utc);

        var medication = new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8),
            IsGiven = true,
            GivenAt = oldGivenAt,
            GivenByUserId = "old-user"
        };

        context.FixedMedications.Add(medication);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);
        var request = new RegisterFixedMedicationGivenRequest { GivenAt = newGivenAt };

        var result = await controller.GiveFixedMedication(
            citizen.CitizenId,
            medication.FixedMedicationId,
            request,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<FixedMedicationDto>(ok.Value);

        Assert.True(dto.IsGiven);
        Assert.Equal(newGivenAt, dto.GivenAt);
        Assert.Equal(employee.IdentityUserId, dto.GivenByUserId);
    }

    [Fact]
    public async Task GiveFixedMedication_ReturnsBadRequest_WhenGivenAtInvalid()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var medication = new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8)
        };

        context.FixedMedications.Add(medication);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);
        var request = new RegisterFixedMedicationGivenRequest { GivenAt = DateTime.MinValue };

        var result = await controller.GiveFixedMedication(
            citizen.CitizenId,
            medication.FixedMedicationId,
            request,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GiveFixedMedication_ReturnsOk_WhenRegisteredWithoutExplicitGivenAt()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var medication = new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8)
        };

        context.FixedMedications.Add(medication);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GiveFixedMedication(
            citizen.CitizenId,
            medication.FixedMedicationId,
            null,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<FixedMedicationDto>(ok.Value);

        Assert.True(dto.IsGiven);
        Assert.Equal(employee.IdentityUserId, dto.GivenByUserId);
        Assert.NotNull(dto.GivenAt);
    }

    [Fact]
    public async Task GiveFixedMedication_ReturnsOk_WhenRegisteredWithExplicitGivenAt()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var medication = new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8)
        };

        context.FixedMedications.Add(medication);
        await context.SaveChangesAsync();

        var givenAt = new DateTime(2026, 4, 27, 10, 30, 0, DateTimeKind.Utc);
        var request = new RegisterFixedMedicationGivenRequest { GivenAt = givenAt };
        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.GiveFixedMedication(
            citizen.CitizenId,
            medication.FixedMedicationId,
            request,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<FixedMedicationDto>(ok.Value);

        Assert.True(dto.IsGiven);
        Assert.Equal(employee.IdentityUserId, dto.GivenByUserId);
        Assert.Equal(givenAt, dto.GivenAt);
    }

    [Fact]
    public async Task CancelFixedMedication_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.CancelFixedMedication(1, 1, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CancelFixedMedication_ReturnsForbid_WhenEmployeeMissingOrDepartmentMissing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "invalid-user");

        var result = await controller.CancelFixedMedication(1, 1, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CancelFixedMedication_ReturnsNotFound_WhenMedicationNotFound()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.CancelFixedMedication(citizen.CitizenId, 999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CancelFixedMedication_ReturnsOk_WhenRegistrationIsCancelled()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var medication = new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8),
            IsGiven = true,
            GivenAt = DateTime.UtcNow,
            GivenByUserId = employee.IdentityUserId
        };

        context.FixedMedications.Add(medication);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.CancelFixedMedication(citizen.CitizenId, medication.FixedMedicationId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<FixedMedicationDto>(ok.Value);

        Assert.False(dto.IsGiven);
        Assert.Null(dto.GivenAt);
        Assert.Null(dto.GivenByUserId);
    }

    [Fact]
    public async Task CancelFixedMedication_ReturnsOkAndKeepsMedicationPlan_WhenRegistrationIsCancelled()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

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
            GivenByUserId = employee.IdentityUserId
        };

        context.FixedMedications.Add(medication);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId);

        var result = await controller.CancelFixedMedication(citizen.CitizenId, medication.FixedMedicationId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<FixedMedicationDto>(ok.Value);

        Assert.False(dto.IsGiven);
        Assert.Null(dto.GivenAt);
        Assert.Null(dto.GivenByUserId);
        Assert.Equal("Panodil 1g", dto.Name);
        Assert.Equal(plannedAt.Hour, dto.PlannedAt.Hour);
        Assert.Equal(plannedAt.Minute, dto.PlannedAt.Minute);
        Assert.Equal("Fast tidspunkt", dto.ScheduleDescription);
        Assert.True(dto.IsActive);

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
    public async Task UpdateFixedMedicationPlan_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.UpdateFixedMedicationPlan(
            1,
            1,
            new UpdateFixedMedicationPlanRequest
            {
                Name = "Panodil",
                PlannedAt = new DateTime(2026, 4, 29, 8, 0, 0),
                ScheduleDescription = "Dagligt",
                IsActive = true
            },
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdateFixedMedicationPlan_ReturnsForbid_WhenUserIsPersonale()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var medication = new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8)
        };

        context.FixedMedications.Add(medication);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId, "Personale");
        var request = new UpdateFixedMedicationPlanRequest
        {
            Name = "Panodil 500mg",
            PlannedAt = new DateTime(2026, 4, 29, 14, 45, 0),
            ScheduleDescription = "Dagligt",
            IsActive = true
        };

        var result = await controller.UpdateFixedMedicationPlan(
            citizen.CitizenId,
            medication.FixedMedicationId,
            request,
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateFixedMedicationPlan_ReturnsOk_WhenPlanIsUpdated()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var medication = new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8)
        };

        context.FixedMedications.Add(medication);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId, "Vagtansvarlig");
        var request = new UpdateFixedMedicationPlanRequest
        {
            Name = "Panodil 500mg",
            PlannedAt = new DateTime(2026, 4, 29, 14, 45, 0),
            ScheduleDescription = "Dagligt",
            IsActive = false
        };

        var result = await controller.UpdateFixedMedicationPlan(
            citizen.CitizenId,
            medication.FixedMedicationId,
            request,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<FixedMedicationDto>(ok.Value);

        Assert.Equal("Panodil 500mg", dto.Name);
        Assert.Equal(14, dto.PlannedAt.Hour);
        Assert.Equal(45, dto.PlannedAt.Minute);
        Assert.Equal("Dagligt", dto.ScheduleDescription);
        Assert.False(dto.IsActive);
    }

    [Fact]
    public async Task UpdateFixedMedicationPlan_ReturnsBadRequest_WhenNameIsMissing()
    {
        using var context = CreateContext();

        var (department, employee) = await CreateDepartmentAndEmployeeAsync(context);

        var citizen = new Citizen
        {
            Name = "Citizen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var medication = new FixedMedication
        {
            CitizenId = citizen.CitizenId,
            Name = "Panodil",
            PlannedAt = DateTime.UtcNow.Date.AddHours(8)
        };

        context.FixedMedications.Add(medication);
        await context.SaveChangesAsync();

        var controller = CreateController(context, employee.IdentityUserId, "Leder");
        var request = new UpdateFixedMedicationPlanRequest
        {
            Name = "",
            PlannedAt = new DateTime(2026, 4, 29, 14, 45, 0),
            ScheduleDescription = "Dagligt",
            IsActive = true
        };

        var result = await controller.UpdateFixedMedicationPlan(
            citizen.CitizenId,
            medication.FixedMedicationId,
            request,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static CitizensApiController CreateController(AppDbContext context, string? userId = null, string role = "Personale")
    {
        var citizenService = new CitizenService(context);
        var fixedMedicationService = new FixedMedicationService(context);

        var controller = new CitizensApiController(
            citizenService,
            fixedMedicationService,
            context,
            NullLogger<CitizensApiController>.Instance);

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
