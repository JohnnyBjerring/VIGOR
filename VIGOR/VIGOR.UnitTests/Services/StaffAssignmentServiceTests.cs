using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class StaffAssignmentServiceTests
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

        var service = new StaffAssignmentService(context);

        var result = await service.GetForCitizenAsync(citizen.CitizenId, depB.DepartmentId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAssignableStaffAsync_ReturnsEmployeesForDepartment()
    {
        using var context = CreateContext();

        var (department, citizen, employee) = await CreateDepartmentCitizenAndEmployeeAsync(context);
        context.Employees.Add(new Employee
        {
            IdentityUserId = "other-user",
            Name = "Anden afdeling",
            DepartmentId = department.DepartmentId + 100
        });
        await context.SaveChangesAsync();

        var service = new StaffAssignmentService(context);

        var result = await service.GetAssignableStaffAsync(citizen.CitizenId, department.DepartmentId);

        Assert.NotNull(result);
        var staff = Assert.Single(result!);
        Assert.Equal(employee.EmployeeId, staff.EmployeeId);
        Assert.Equal(employee.Name, staff.Name);
    }

    [Fact]
    public async Task AssignAsync_CreatesAssignment_WhenCitizenAndEmployeeAreInDepartment()
    {
        using var context = CreateContext();

        var (department, citizen, employee) = await CreateDepartmentCitizenAndEmployeeAsync(context);
        var service = new StaffAssignmentService(context);

        var result = await service.AssignAsync(
            citizen.CitizenId,
            department.DepartmentId,
            employee.EmployeeId,
            "leader-user");

        Assert.NotNull(result);
        Assert.Equal(citizen.CitizenId, result!.CitizenId);
        Assert.Equal(department.DepartmentId, result.DepartmentId);
        Assert.Equal(employee.EmployeeId, result.EmployeeId);
        Assert.Equal(employee.Name, result.EmployeeNameSnapshot);
        Assert.Equal("leader-user", result.AssignedByUserId);
        Assert.True(result.IsActive);

        var dbAssignment = await context.CitizenStaffAssignments.SingleAsync();
        Assert.Equal(employee.EmployeeId, dbAssignment.EmployeeId);
        Assert.True(dbAssignment.IsActive);
    }

    [Fact]
    public async Task AssignAsync_ReturnsExistingAssignment_WhenSameEmployeeAlreadyActive()
    {
        using var context = CreateContext();

        var (department, citizen, employee) = await CreateDepartmentCitizenAndEmployeeAsync(context);
        var service = new StaffAssignmentService(context);

        var first = await service.AssignAsync(citizen.CitizenId, department.DepartmentId, employee.EmployeeId, "leader-user");
        var second = await service.AssignAsync(citizen.CitizenId, department.DepartmentId, employee.EmployeeId, "leader-user");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.CitizenStaffAssignmentId, second!.CitizenStaffAssignmentId);
        Assert.Equal(1, await context.CitizenStaffAssignments.CountAsync());
    }

    [Fact]
    public async Task AssignAsync_ReturnsNull_WhenEmployeeNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = depA.DepartmentId };
        var employee = new Employee { IdentityUserId = "staff-user", Name = "Staff", DepartmentId = depB.DepartmentId };
        context.Citizens.Add(citizen);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var service = new StaffAssignmentService(context);

        var result = await service.AssignAsync(citizen.CitizenId, depA.DepartmentId, employee.EmployeeId, "leader-user");

        Assert.Null(result);
        Assert.Empty(context.CitizenStaffAssignments);
    }

    [Fact]
    public async Task AssignAsync_WritesAuditEvent_WhenAuditServiceIsInjected()
    {
        using var context = CreateContext();

        var (department, citizen, employee) = await CreateDepartmentCitizenAndEmployeeAsync(context);
        var auditService = new AuditService(context);
        var service = new StaffAssignmentService(context, auditService);

        var result = await service.AssignAsync(
            citizen.CitizenId,
            department.DepartmentId,
            employee.EmployeeId,
            "leader-user",
            userDisplayNameSnapshot: "Leder Jensen");

        Assert.NotNull(result);

        var auditEvent = await context.AuditEvents.SingleAsync();
        Assert.Equal("StaffAssignedToCitizen", auditEvent.Action);
        Assert.Equal("CitizenStaffAssignment", auditEvent.EntityType);
        Assert.Equal(result!.CitizenStaffAssignmentId, auditEvent.EntityId);
        Assert.Equal(citizen.CitizenId, auditEvent.CitizenId);
        Assert.Equal(department.DepartmentId, auditEvent.DepartmentId);
        Assert.Equal("leader-user", auditEvent.UserId);
        Assert.Equal("Leder Jensen", auditEvent.UserDisplayNameSnapshot);
    }

    [Fact]
    public async Task UnassignAsync_DeactivatesAssignment_WhenAssignmentExists()
    {
        using var context = CreateContext();

        var (department, citizen, employee) = await CreateDepartmentCitizenAndEmployeeAsync(context);
        var service = new StaffAssignmentService(context);
        var assigned = await service.AssignAsync(citizen.CitizenId, department.DepartmentId, employee.EmployeeId, "leader-user");

        var result = await service.UnassignAsync(
            citizen.CitizenId,
            assigned!.CitizenStaffAssignmentId,
            department.DepartmentId,
            "leader-user-2");

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
        Assert.NotNull(result.UnassignedAtUtc);
        Assert.Equal("leader-user-2", result.UnassignedByUserId);

        var dbAssignment = await context.CitizenStaffAssignments.SingleAsync();
        Assert.False(dbAssignment.IsActive);
        Assert.Equal("leader-user-2", dbAssignment.UnassignedByUserId);
    }

    [Fact]
    public async Task UnassignAsync_ReturnsNull_WhenAssignmentNotInDepartment()
    {
        using var context = CreateContext();

        var depA = new Department { Name = "A" };
        var depB = new Department { Name = "B" };
        context.Departments.AddRange(depA, depB);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = depA.DepartmentId };
        context.Citizens.Add(citizen);
        await context.SaveChangesAsync();

        var service = new StaffAssignmentService(context);

        var result = await service.UnassignAsync(citizen.CitizenId, 123, depB.DepartmentId, "leader-user");

        Assert.Null(result);
    }

    [Fact]
    public async Task UnassignAsync_WritesAuditEvent_WhenAuditServiceIsInjected()
    {
        using var context = CreateContext();

        var (department, citizen, employee) = await CreateDepartmentCitizenAndEmployeeAsync(context);
        var auditService = new AuditService(context);
        var service = new StaffAssignmentService(context, auditService);
        var assigned = await service.AssignAsync(citizen.CitizenId, department.DepartmentId, employee.EmployeeId, "leader-user");
        context.AuditEvents.RemoveRange(context.AuditEvents);
        await context.SaveChangesAsync();

        var result = await service.UnassignAsync(
            citizen.CitizenId,
            assigned!.CitizenStaffAssignmentId,
            department.DepartmentId,
            "leader-user-2",
            userDisplayNameSnapshot: "Vagtansvarlig Jensen");

        Assert.NotNull(result);

        var auditEvent = await context.AuditEvents.SingleAsync();
        Assert.Equal("StaffUnassignedFromCitizen", auditEvent.Action);
        Assert.Equal("CitizenStaffAssignment", auditEvent.EntityType);
        Assert.Equal(assigned.CitizenStaffAssignmentId, auditEvent.EntityId);
        Assert.Equal(citizen.CitizenId, auditEvent.CitizenId);
        Assert.Equal(department.DepartmentId, auditEvent.DepartmentId);
        Assert.Equal("leader-user-2", auditEvent.UserId);
        Assert.Equal("Vagtansvarlig Jensen", auditEvent.UserDisplayNameSnapshot);
    }

    private static async Task<(Department Department, Citizen Citizen, Employee Employee)> CreateDepartmentCitizenAndEmployeeAsync(AppDbContext context)
    {
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var citizen = new Citizen { Name = "Citizen", DepartmentId = department.DepartmentId };
        var employee = new Employee
        {
            IdentityUserId = "staff-user",
            Name = "Medarbejder Jensen",
            DepartmentId = department.DepartmentId
        };

        context.Citizens.Add(citizen);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        return (department, citizen, employee);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
