using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Models;
using VIGOR.Web.Controllers.Api;
using VIGOR.Web.Data;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Controllers;

public class WorkPhonesApiControllerTests
{
    [Fact]
    public async Task GetPhones_ReturnsOk()
    {
        using var context = CreateContext();
        context.WorkPhones.Add(new WorkPhone { Label = "Telefon 1", PhoneNumber = "12345678" });
        await context.SaveChangesAsync();
        var controller = CreateController(context, "leader-user", "Leder");

        var result = await controller.GetPhones(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var phones = Assert.IsAssignableFrom<IReadOnlyList<WorkPhoneDto>>(ok.Value);
        Assert.Single(phones);
    }

    [Fact]
    public async Task CreatePhone_ReturnsCreated_WhenRequestIsValid()
    {
        using var context = CreateContext();
        var controller = CreateController(context, "leader-user", "Leder");

        var result = await controller.CreatePhone(new CreateWorkPhoneRequest
        {
            Label = "Telefon 1",
            PhoneNumber = "12345678"
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<WorkPhoneDto>(created.Value);
        Assert.Equal("Telefon 1", dto.Label);
        Assert.Equal("12345678", dto.PhoneNumber);
        Assert.Equal(1, await context.WorkPhones.CountAsync());
    }

    [Fact]
    public async Task AssignPhone_ReturnsUnauthorized_WhenNoUser()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.AssignPhone(new AssignWorkPhoneRequest
        {
            WorkPhoneId = 1,
            EmployeeId = 1
        }, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task AssignPhone_ReturnsCreated_WhenRequestIsValid()
    {
        using var context = CreateContext();
        var employee = await CreateEmployeeAsync(context);
        var phone = await CreatePhoneAsync(context);
        var controller = CreateController(context, "leader-user", "Leder");

        var result = await controller.AssignPhone(new AssignWorkPhoneRequest
        {
            WorkPhoneId = phone.WorkPhoneId,
            EmployeeId = employee.EmployeeId
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<PhoneAssignmentDto>(created.Value);
        Assert.Equal(phone.WorkPhoneId, dto.WorkPhoneId);
        Assert.Equal(employee.EmployeeId, dto.EmployeeId);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task UnassignPhone_ReturnsOk_WhenAssignmentExists()
    {
        using var context = CreateContext();
        var employee = await CreateEmployeeAsync(context);
        var phone = await CreatePhoneAsync(context);
        var service = new WorkPhoneService(context);
        var assignment = await service.AssignPhoneAsync(new AssignWorkPhoneRequest
        {
            WorkPhoneId = phone.WorkPhoneId,
            EmployeeId = employee.EmployeeId
        }, "leader-user");
        var controller = CreateController(context, "leader-user", "Leder");

        var result = await controller.UnassignPhone(assignment!.PhoneAssignmentId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PhoneAssignmentDto>(ok.Value);
        Assert.False(dto.IsActive);
    }

    private static WorkPhonesApiController CreateController(AppDbContext context, string? userId = null, string role = "Leder")
    {
        var service = new WorkPhoneService(context);
        var controller = new WorkPhonesApiController(service, NullLogger<WorkPhonesApiController>.Instance);

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

    private static async Task<Employee> CreateEmployeeAsync(AppDbContext context)
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
        return employee;
    }

    private static async Task<WorkPhone> CreatePhoneAsync(AppDbContext context)
    {
        var phone = new WorkPhone
        {
            Label = "Telefon 1",
            PhoneNumber = "12345678",
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
