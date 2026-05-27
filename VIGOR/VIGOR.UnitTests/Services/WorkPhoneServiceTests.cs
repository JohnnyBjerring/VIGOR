using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Services;

public class WorkPhoneServiceTests
{
    [Fact]
    public async Task CreatePhoneAsync_CreatesWorkPhone()
    {
        using var context = CreateContext();
        var service = new WorkPhoneService(context);

        var result = await service.CreatePhoneAsync(new CreateWorkPhoneRequest
        {
            Label = "Telefon 1",
            PhoneNumber = "12345678"
        });

        Assert.Equal("Telefon 1", result.Label);
        Assert.Equal("12345678", result.PhoneNumber);
        Assert.True(result.IsActive);
        Assert.False(result.IsAssigned);
        Assert.Equal(1, await context.WorkPhones.CountAsync());
    }

    [Fact]
    public async Task CreatePhoneAsync_ThrowsConflict_WhenPhoneNumberAlreadyExists()
    {
        using var context = CreateContext();
        var service = new WorkPhoneService(context);

        await service.CreatePhoneAsync(new CreateWorkPhoneRequest { Label = "Telefon 1", PhoneNumber = "12345678" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePhoneAsync(new CreateWorkPhoneRequest
        {
            Label = "Telefon 2",
            PhoneNumber = "12345678"
        }));
    }

    [Fact]
    public async Task AssignPhoneAsync_CreatesActiveAssignment()
    {
        using var context = CreateContext();
        var (_, employee) = await CreateDepartmentAndEmployeeAsync(context);
        var phone = await CreatePhoneAsync(context);
        var service = new WorkPhoneService(context);

        var result = await service.AssignPhoneAsync(new AssignWorkPhoneRequest
        {
            WorkPhoneId = phone.WorkPhoneId,
            EmployeeId = employee.EmployeeId
        }, "leader-user");

        Assert.NotNull(result);
        Assert.Equal(phone.WorkPhoneId, result!.WorkPhoneId);
        Assert.Equal(employee.EmployeeId, result.EmployeeId);
        Assert.True(result.IsActive);
        Assert.Equal("leader-user", result.AssignedByUserId);

        var dbAssignment = await context.PhoneAssignments.SingleAsync();
        Assert.True(dbAssignment.IsActive);
        Assert.Equal(employee.Name, dbAssignment.EmployeeNameSnapshot);
    }

    [Fact]
    public async Task AssignPhoneAsync_DeactivatesPreviousAssignmentForSameEmployee()
    {
        using var context = CreateContext();
        var (_, employee) = await CreateDepartmentAndEmployeeAsync(context);
        var phone1 = await CreatePhoneAsync(context, "Telefon 1", "11111111");
        var phone2 = await CreatePhoneAsync(context, "Telefon 2", "22222222");
        var service = new WorkPhoneService(context);

        var first = await service.AssignPhoneAsync(new AssignWorkPhoneRequest { WorkPhoneId = phone1.WorkPhoneId, EmployeeId = employee.EmployeeId }, "leader-user");
        var second = await service.AssignPhoneAsync(new AssignWorkPhoneRequest { WorkPhoneId = phone2.WorkPhoneId, EmployeeId = employee.EmployeeId }, "leader-user");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first!.PhoneAssignmentId, second!.PhoneAssignmentId);

        var assignments = await context.PhoneAssignments.OrderBy(a => a.PhoneAssignmentId).ToListAsync();
        Assert.Equal(2, assignments.Count);
        Assert.False(assignments[0].IsActive);
        Assert.True(assignments[1].IsActive);
    }

    [Fact]
    public async Task UnassignPhoneAsync_DeactivatesAssignment()
    {
        using var context = CreateContext();
        var (_, employee) = await CreateDepartmentAndEmployeeAsync(context);
        var phone = await CreatePhoneAsync(context);
        var service = new WorkPhoneService(context);
        var assigned = await service.AssignPhoneAsync(new AssignWorkPhoneRequest { WorkPhoneId = phone.WorkPhoneId, EmployeeId = employee.EmployeeId }, "leader-user");

        var result = await service.UnassignPhoneAsync(assigned!.PhoneAssignmentId, "leader-user-2");

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
        Assert.NotNull(result.UnassignedAtUtc);
        Assert.Equal("leader-user-2", result.UnassignedByUserId);

        var dbAssignment = await context.PhoneAssignments.SingleAsync();
        Assert.False(dbAssignment.IsActive);
    }

    [Fact]
    public async Task GetAssignableEmployeesAsync_ReturnsActivePhoneDisplay()
    {
        using var context = CreateContext();
        var (_, employee) = await CreateDepartmentAndEmployeeAsync(context);
        var phone = await CreatePhoneAsync(context, "Telefon 1", "12345678");
        var service = new WorkPhoneService(context);
        await service.AssignPhoneAsync(new AssignWorkPhoneRequest { WorkPhoneId = phone.WorkPhoneId, EmployeeId = employee.EmployeeId }, "leader-user");

        var result = await service.GetAssignableEmployeesAsync();

        var dto = Assert.Single(result);
        Assert.Equal(employee.EmployeeId, dto.EmployeeId);
        Assert.Equal("Telefon 1 (12345678)", dto.ActivePhoneDisplayName);
    }

    private static async Task<(Department Department, Employee Employee)> CreateDepartmentAndEmployeeAsync(AppDbContext context)
    {
        var department = new Department { Name = "A" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var employee = new Employee
        {
            IdentityUserId = "employee-user",
            Name = "Medarbejder Jensen",
            DepartmentId = department.DepartmentId
        };

        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        return (department, employee);
    }

    private static async Task<WorkPhone> CreatePhoneAsync(AppDbContext context, string label = "Telefon 1", string phoneNumber = "12345678")
    {
        var phone = new WorkPhone
        {
            Label = label,
            PhoneNumber = phoneNumber,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.WorkPhones.Add(phone);
        await context.SaveChangesAsync();
        return phone;
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
